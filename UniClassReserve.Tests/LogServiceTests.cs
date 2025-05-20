using System;
using System.Collections.Generic;
using Xunit;

namespace UniClassReserve.Tests
{
    public class LogServiceTests
    {
        [Fact]
        public void Can_Add_Info_Log()
        {
            // Arrange
            var logs = new List<LogEntryMock>();
            var logService = new LogServiceMock(logs);
            // Act
            logService.Log("user1", "Test info log", false, "details", "Info");
            // Assert
            Assert.Single(logs);
            Assert.Equal("Info", logs[0].LogLevel);
            Assert.Equal("Test info log", logs[0].Message);
        }

        [Fact]
        public void Can_Add_Error_Log()
        {
            // Arrange
            var logs = new List<LogEntryMock>();
            var logService = new LogServiceMock(logs);
            // Act
            logService.Log("user1", "Test error log", true, "details", "Error");
            // Assert
            Assert.Single(logs);
            Assert.Equal("Error", logs[0].LogLevel);
            Assert.True(logs[0].IsError);
        }

        [Fact]
        public void LogService_Stores_Multiple_Logs()
        {
            // Arrange
            var logs = new List<LogEntryMock>();
            var logService = new LogServiceMock(logs);
            // Act
            logService.Log("user1", "Log1", false, "-", "Info");
            logService.Log("user2", "Log2", true, "-", "Error");
            // Assert
            Assert.Equal(2, logs.Count);
            Assert.Contains(logs, l => l.LogLevel == "Error");
            Assert.Contains(logs, l => l.LogLevel == "Info");
        }
    }

    // Mock log entry
    public class LogEntryMock
    {
        public string UserId { get; set; }
        public string Message { get; set; }
        public bool IsError { get; set; }
        public string Details { get; set; }
        public string LogLevel { get; set; }
    }

    // Mock log service
    public class LogServiceMock
    {
        private readonly List<LogEntryMock> _logs;
        public LogServiceMock(List<LogEntryMock> logs)
        {
            _logs = logs;
        }
        public void Log(string userId, string message, bool isError, string details, string logLevel)
        {
            _logs.Add(new LogEntryMock
            {
                UserId = userId,
                Message = message,
                IsError = isError,
                Details = details,
                LogLevel = logLevel
            });
        }
    }
} 