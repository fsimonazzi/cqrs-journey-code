// ==============================================================================================================
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

namespace Registration.IntegrationTests
{
    using System;
    using Registration.ReadModel.Implementation;

    public class given_a_read_model_database : IDisposable
    {
        protected string conferenceDbName;
        protected string pricedOrderDbName;
        protected string draftOrderDbName;

        public given_a_read_model_database()
        {
            this.conferenceDbName = this.CreateDb();
            this.pricedOrderDbName = this.CreateDb();
            this.draftOrderDbName = this.CreateDb();
        }

        public void Dispose()
        {
            this.DropDb(this.conferenceDbName);
            this.DropDb(this.pricedOrderDbName);
            this.DropDb(this.draftOrderDbName);
        }

        private string CreateDb()
        {
            var dbName = this.GetType().Name + "-" + Guid.NewGuid().ToString();
            using (var context = new ConferenceRegistrationDbContext(dbName))
            {
                if (context.Database.Exists())
                    context.Database.Delete();

                context.Database.Create();
            }

            return dbName;
        }

        private void DropDb(string dbName)
        {
            using (var context = new ConferenceRegistrationDbContext(dbName))
            {
                if (context.Database.Exists())
                    context.Database.Delete();
            }
        }
    }
}
