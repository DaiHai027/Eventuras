using losol.EventManagement.Infrastructure;
using Losol.Communication.Email;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace losol.EventManagement.IntegrationTests.Controllers.Api
{
    public class MessagingControllerTest : IClassFixture<CustomWebApplicationFactory<Startup>>
    {
        private readonly CustomWebApplicationFactory<Startup> factory;

        public MessagingControllerTest(CustomWebApplicationFactory<Startup> factory)
        {
            this.factory = factory;
        }

        [Theory]
        [InlineData("nb-NO", "Velkommen til")]
        [InlineData("en-US", "Welcome to")]
        public async Task Should_Send_Register_Email(string languageCode, string textToCheck)
        {
            var client = this.factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            await client.LogInAsSuperAdminAsync();

            using var scope = this.factory.Services.NewScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var eventInfo = SeedData.Events[0];
            using var user = await scope.ServiceProvider.NewUserAsync();
            using var registration = await context.NewRegistrationAsync(eventInfo, user.Entity);

            var emailExpectation = this.factory.EmailSenderMock
                .ExpectEmail()
                .SentTo(user.Entity.Email)
                .WithSubject("Test")
                .ContainingText("Test Email Contents")
                .ContainingText(textToCheck)
                .Setup();

            client.AcceptLanguage(languageCode);
            var response = await client.PostAsync($"/api/v0/messaging/email/participants-at-event/{eventInfo.EventInfoId}",
                new StringContent(JsonConvert.SerializeObject(new
                {
                    Subject = "Test",
                    Content = "Test Email Contents"
                }), Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("Sendte epost. Men fikk noen feil", content);

            emailExpectation.VerifyEmailSent(Times.Once());
        }
    }
}
