﻿// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
// http://cqrsjourney.github.com/contributors/members
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

namespace Conference.Web.Public.Controllers
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Web.Mvc;
    using Conference.Web.Public.Models;
    using Infrastructure.Messaging;
    using Payments.Contracts.Commands;
    using Registration.Commands;
    using Registration.ReadModel;

    public class RegistrationController : ConferenceTenantController
    {
        public const string ThirdPartyProcessorPayment = "thirdParty";
        public const string InvoicePayment = "invoice";
        private const int DraftOrderWaitTimeoutInSeconds = 5;
        private const int PricedOrderWaitTimeoutInSeconds = 1;

        private readonly ICommandBus commandBus;
        private readonly IOrderDao orderDao;

        public RegistrationController(ICommandBus commandBus, IOrderDao orderDao, IConferenceDao conferenceDao)
            : base(conferenceDao)
        {
            this.commandBus = commandBus;
            this.orderDao = orderDao;
        }

        [HttpGet]
        [OutputCache(Duration = 0)]
        public ActionResult StartRegistration(Guid? orderId = null)
        {
            OrderViewModel viewModel;
            int orderVersion = 0;

            if (!orderId.HasValue)
            {
                orderId = Guid.NewGuid();
                viewModel = this.CreateViewModel();
                this.ViewBag.ExpirationDateUTC = DateTime.MinValue;
                ViewBag.PartiallFulfilled = false;
            }
            else
            {
                var order = this.orderDao.GetDraftOrder(orderId.Value);

                if (order.State == DraftOrder.States.Confirmed)
                {
                    return View("ShowCompletedOrder");
                }

                orderVersion = order.OrderVersion;
                viewModel = this.CreateViewModel(order);
                ViewBag.ExpirationDateUTC = order.ReservationExpirationDate;
                ViewBag.PartiallFulfilled = order.State == DraftOrder.States.PartiallyReserved;
            }

            ViewBag.OrderId = orderId;
            ViewBag.OrderVersion = orderVersion;

            return View(viewModel);
        }

        [HttpPost]
        public ActionResult StartRegistration(RegisterToConference command, int orderVersion)
        {
            if (!ModelState.IsValid)
            {
                return StartRegistration(command.OrderId);
            }

            // TODO: validate incoming seat types correspond to the conference.

            command.ConferenceId = this.ConferenceAlias.Id;
            this.commandBus.Send(command);

            return RedirectToAction(
                "SpecifyRegistrantAndPaymentDetails",
                new { conferenceCode = this.ConferenceCode, orderId = command.OrderId, orderVersion = orderVersion });
        }

        [HttpGet]
        [OutputCache(Duration = 0)]
        public ActionResult SpecifyRegistrantAndPaymentDetails(Guid orderId, int orderVersion)
        {
            var order = this.WaitUntilSeatsAreConfirmed(orderId, orderVersion);
            if (order == null)
            {
                return View("ReservationUnknown");
            }

            if (order.State == DraftOrder.States.Rejected)
            {
                return View("ReservationRejected");
            }

            if (order.State == DraftOrder.States.PartiallyReserved)
            {
                return this.RedirectToAction("StartRegistration", new { conferenceCode = this.ConferenceCode, orderId, orderVersion = order.OrderVersion });
            }

            if (order.State == DraftOrder.States.Confirmed)
            {
                return View("ShowCompletedOrder");
            }

            if (order.ReservationExpirationDate.HasValue && order.ReservationExpirationDate < DateTime.UtcNow)
            {
                return RedirectToAction("ShowExpiredOrder", new { conferenceCode = this.ConferenceAlias.Code, orderId = orderId });
            }

            var pricedOrder = this.WaitUntilOrderIsPriced(orderId, orderVersion);
            if (pricedOrder == null)
            {
                return View("ReservationUnknown");
            }

            // NOTE: we use the view bag to pass out of band details needed for the UI.
            this.ViewBag.ExpirationDateUTC = order.ReservationExpirationDate;

            // We just render the command which is later posted back.
            return View(
                new RegistrationViewModel
                {
                    RegistrantDetails = new AssignRegistrantDetails { OrderId = orderId },
                    Order = pricedOrder
                });
        }

        [HttpPost]
        public ActionResult SpecifyRegistrantAndPaymentDetails(AssignRegistrantDetails command, string paymentType, int orderVersion)
        {
            var orderId = command.OrderId;

            if (!ModelState.IsValid)
            {
                return SpecifyRegistrantAndPaymentDetails(orderId, orderVersion);
            }

            var order = this.orderDao.GetDraftOrder(orderId);

            // TODO check conference and order exist.
            // TODO validate that order belongs to the user.

            if (order == null)
            {
                throw new ArgumentException();
            }

            if (order.ReservationExpirationDate.HasValue && order.ReservationExpirationDate < DateTime.UtcNow)
            {
                return RedirectToAction("ShowExpiredOrder", new { conferenceCode = this.ConferenceAlias.Code, orderId = orderId });
            }

            switch (paymentType)
            {
                case ThirdPartyProcessorPayment:

                    return InitiateRegistrationWithThirdPartyProcessorPayment(command, orderId, orderVersion);

                case InvoicePayment:
                    break;

                default:
                    break;
            }

            throw new InvalidOperationException();
        }

        [HttpGet]
        [OutputCache(Duration = 0)]
        public ActionResult ShowExpiredOrder(Guid orderId)
        {
            return View();
        }

        [HttpGet]
        [OutputCache(Duration = 0)]
        public ActionResult ThankYou(Guid orderId)
        {
            var order = this.orderDao.GetDraftOrder(orderId);

            return View(order);
        }

        private ActionResult InitiateRegistrationWithThirdPartyProcessorPayment(AssignRegistrantDetails command, Guid orderId, int orderVersion)
        {
            var paymentCommand = CreatePaymentCommand(orderId);

            this.commandBus.Send(new ICommand[] { command, paymentCommand });

            var paymentAcceptedUrl = this.Url.Action("ThankYou", new { conferenceCode = this.ConferenceAlias.Code, orderId });
            var paymentRejectedUrl = this.Url.Action("SpecifyRegistrantAndPaymentDetails", new { conferenceCode = this.ConferenceAlias.Code, orderId, orderVersion });

            return RedirectToAction(
                "ThirdPartyProcessorPayment",
                "Payment",
                new
                {
                    conferenceCode = this.ConferenceAlias.Code,
                    paymentId = paymentCommand.PaymentId,
                    paymentAcceptedUrl,
                    paymentRejectedUrl
                });
        }

        private InitiateThirdPartyProcessorPayment CreatePaymentCommand(Guid orderId)
        {
            var totalledOrder = this.orderDao.GetPricedOrder(orderId);
            // TODO: should add the line items?

            var description = "Registration for " + this.ConferenceAlias.Name;
            var totalAmount = totalledOrder.Total;

            var paymentCommand =
                new InitiateThirdPartyProcessorPayment
                {
                    PaymentId = Guid.NewGuid(),
                    ConferenceId = this.ConferenceAlias.Id,
                    PaymentSourceId = orderId,
                    Description = description,
                    TotalAmount = totalAmount
                };

            return paymentCommand;
        }

        private OrderViewModel CreateViewModel()
        {
            var seatTypes = this.ConferenceDao.GetPublishedSeatTypes(this.ConferenceAlias.Id);
            var viewModel =
                new OrderViewModel
                {
                    ConferenceId = this.ConferenceAlias.Id,
                    ConferenceCode = this.ConferenceAlias.Code,
                    ConferenceName = this.ConferenceAlias.Name,
                    Items =
                        seatTypes.Select(
                            s =>
                                new OrderItemViewModel
                                {
                                    SeatType = s,
                                    OrderItem = new DraftOrderItem(s.Id, 0),
                                    MaxSeatSelection = 20
                                }).ToList(),
                };

            return viewModel;
        }

        private OrderViewModel CreateViewModel(DraftOrder order)
        {
            var viewModel = this.CreateViewModel();
            viewModel.Id = order.OrderId;

            // TODO check DTO matches view model

            foreach (var line in order.Lines)
            {
                var seat = viewModel.Items.First(s => s.SeatType.Id == line.SeatType);
                seat.OrderItem = line;
                if (line.RequestedSeats > line.ReservedSeats)
                {
                    seat.PartiallyFulfilled = true;
                    seat.MaxSeatSelection = line.ReservedSeats;
                }
            }

            return viewModel;
        }

        private DraftOrder WaitUntilSeatsAreConfirmed(Guid orderId, int lastOrderVersion)
        {
            var deadline = DateTime.Now.AddSeconds(DraftOrderWaitTimeoutInSeconds);

            while (DateTime.Now < deadline)
            {
                var order = this.orderDao.GetDraftOrder(orderId);

                if (order != null && order.State != DraftOrder.States.PendingReservation && order.OrderVersion > lastOrderVersion)
                {
                    return order;
                }

                Thread.Sleep(500);
            }

            return null;
        }

        private PricedOrder WaitUntilOrderIsPriced(Guid orderId, int lastOrderVersion)
        {
            var deadline = DateTime.Now.AddSeconds(PricedOrderWaitTimeoutInSeconds);

            while (DateTime.Now < deadline)
            {
                var order = this.orderDao.GetPricedOrder(orderId);
                if (order != null && order.OrderVersion > lastOrderVersion)
                {
                    return order;
                }

                Thread.Sleep(300);
            }

            return null;
        }
    }
}
