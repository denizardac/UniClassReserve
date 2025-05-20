using System;
using System.Collections.Generic;
using Xunit;

namespace UniClassReserve.Tests
{
    public class UserValidationTests
    {
        [Fact]
        public void User_With_Valid_Id_Is_Authenticated()
        {
            // Arrange
            var user = new UserMock { Id = "user1", Roles = new List<string> { "Instructor" } };
            // Act
            bool isAuthenticated = !string.IsNullOrEmpty(user.Id);
            // Assert
            Assert.True(isAuthenticated);
        }

        [Fact]
        public void User_With_Admin_Role_Is_Admin()
        {
            // Arrange
            var user = new UserMock { Id = "admin1", Roles = new List<string> { "Admin", "Instructor" } };
            // Act
            bool isAdmin = user.Roles.Contains("Admin");
            // Assert
            Assert.True(isAdmin);
        }

        [Fact]
        public void User_Without_Required_Role_Is_Denied()
        {
            // Arrange
            var user = new UserMock { Id = "user2", Roles = new List<string> { "Instructor" } };
            // Act
            bool isAdmin = user.Roles.Contains("Admin");
            // Assert
            Assert.False(isAdmin);
        }

        [Fact]
        public void Unauthenticated_User_Is_Denied()
        {
            // Arrange
            var user = new UserMock { Id = null, Roles = new List<string>() };
            // Act
            bool isAuthenticated = !string.IsNullOrEmpty(user.Id);
            // Assert
            Assert.False(isAuthenticated);
        }
    }

    // Mock user
    public class UserMock
    {
        public string Id { get; set; }
        public List<string> Roles { get; set; }
    }
} 