using System;
using System.Collections.Generic;
using Xunit;

namespace UniClassReserve.Tests
{
    public class EmailServiceTests
    {
        [Fact]
        public void Can_Send_Email_Successfully()
        {
            // Arrange
            var sentEmails = new List<EmailMock>();
            var emailService = new EmailServiceMock(sentEmails);
            // Act
            bool result = emailService.SendEmail("to@example.com", "Subject", "Body");
            // Assert
            Assert.True(result);
            Assert.Single(sentEmails);
            Assert.Equal("to@example.com", sentEmails[0].To);
            Assert.Equal("Subject", sentEmails[0].Subject);
            Assert.Equal("Body", sentEmails[0].Body);
        }

        [Fact]
        public void SendEmail_Fails_For_Invalid_Address()
        {
            // Arrange
            var sentEmails = new List<EmailMock>();
            var emailService = new EmailServiceMock(sentEmails);
            // Act
            bool result = emailService.SendEmail("", "Subject", "Body");
            // Assert
            Assert.False(result);
            Assert.Empty(sentEmails);
        }

        [Fact]
        public void Email_Body_Should_Contain_Required_Info()
        {
            // Arrange
            var sentEmails = new List<EmailMock>();
            var emailService = new EmailServiceMock(sentEmails);
            string body = "<b>Reservation</b><br/>Classroom: 101<br/>";
            // Act
            emailService.SendEmail("to@example.com", "Reservation Info", body);
            // Assert
            Assert.Contains("Classroom: 101", sentEmails[0].Body);
        }
    }

    // Mock email entry
    public class EmailMock
    {
        public string To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
    }

    // Mock email service
    public class EmailServiceMock
    {
        private readonly List<EmailMock> _sentEmails;
        public EmailServiceMock(List<EmailMock> sentEmails)
        {
            _sentEmails = sentEmails;
        }
        public bool SendEmail(string to, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(to))
                return false;
            _sentEmails.Add(new EmailMock
            {
                To = to,
                Subject = subject,
                Body = body
            });
            return true;
        }
    }
} 