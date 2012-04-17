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

namespace Payments.Tests.ThirdPartyProcessorPaymentCommandHandlerFixture
{
    using System;
    using System.Linq;
    using Common;
    using Moq;
    using Payments.Commands;
    using Payments.Handlers;
    using Xunit;

    public class given_no_payment
    {
        private Mock<IRepository<ThirdPartyProcessorPayment>> repositoryMock;
        private ThirdPartyProcessorPaymentCommandHandler handler;

        public given_no_payment()
        {
            this.repositoryMock = new Mock<IRepository<ThirdPartyProcessorPayment>>();
            this.handler = new ThirdPartyProcessorPaymentCommandHandler(() => this.repositoryMock.Object);
        }

        [Fact]
        public void when_initiating_payment_then_adds_new_payment()
        {
            ThirdPartyProcessorPayment payment = null;
            var orderId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();
            var conferenceId = Guid.NewGuid();

            this.repositoryMock
                .Setup(x => x.Save(It.IsAny<ThirdPartyProcessorPayment>()))
                .Callback<ThirdPartyProcessorPayment>(p => payment = p);

            this.handler.Handle(
                new InitiateThirdPartyProcessorPayment
                {
                    PaymentId = paymentId,
                    SourceId = orderId,
                    ConferenceId = conferenceId,
                    Items = 
                    { 
                        new InitiateThirdPartyProcessorPayment.PaymentItem{ Description = "payment", Amount = 100d } 
                    }
                });

            Assert.NotNull(payment);
            Assert.Equal(1, payment.Items.Count);
            Assert.Equal("payment", payment.Items.ElementAt(0).Description);
            Assert.Equal(100d, payment.Items.ElementAt(0).Amount);
        }
    }

    public class given_initiated_payment
    {
        private Mock<IRepository<ThirdPartyProcessorPayment>> repositoryMock;
        private ThirdPartyProcessorPayment payment;
        private ThirdPartyProcessorPaymentCommandHandler handler;

        public given_initiated_payment()
        {
            this.repositoryMock = new Mock<IRepository<ThirdPartyProcessorPayment>>();
            this.payment = new ThirdPartyProcessorPayment(Guid.NewGuid(), Guid.NewGuid(), "payment", 100, new ThidPartyProcessorPaymentItem[0]);
            this.handler = new ThirdPartyProcessorPaymentCommandHandler(() => this.repositoryMock.Object);

            repositoryMock.Setup(x => x.Find(payment.Id)).Returns(payment);
        }

        [Fact]
        public void when_completing_payment_then_updates_payment()
        {
            this.handler.Handle(
                new CompleteThirdPartyProcessorPayment
                {
                    PaymentId = this.payment.Id
                });

            Assert.Equal(ThirdPartyProcessorPayment.States.Completed, this.payment.State);
            this.repositoryMock.Verify(r => r.Save(this.payment), Times.Once());
        }

        [Fact]
        public void when_cancelling_payment_then_updates_payment()
        {
            this.handler.Handle(
                new CancelThirdPartyProcessorPayment
                {
                    PaymentId = this.payment.Id
                });

            Assert.Equal(ThirdPartyProcessorPayment.States.Rejected, this.payment.State);
            this.repositoryMock.Verify(r => r.Save(this.payment), Times.Once());
        }
    }
}