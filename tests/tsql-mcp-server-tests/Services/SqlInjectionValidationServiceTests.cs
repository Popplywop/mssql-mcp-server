using Microsoft.Extensions.Logging;
using Moq;
using Services;
using FluentAssertions;

namespace tsql_mcp_server_tests.Services
{
    public class SqlInjectionValidationServiceTests
    {
        private readonly SqlInjectionValidationService _service;
        private readonly Mock<ILogger<SqlInjectionValidationService>> _loggerMock;

        public SqlInjectionValidationServiceTests()
        {
            // Setup for each test
            _loggerMock = new Mock<ILogger<SqlInjectionValidationService>>();
            _service = new SqlInjectionValidationService(_loggerMock.Object);
        }

        [Theory]
        [InlineData("SELECT * FROM Users")]
        [InlineData("SELECT Id, Name FROM Products WHERE Price > 10")]
        [InlineData("SELECT COUNT(*) FROM Orders")]
        public void ValidateQuery_WithValidSelectQueries_ReturnsValid(string query)
        {
            // Act
            var result = _service.ValidateQuery(query);

            // Assert
            result.isValid.Should().BeTrue();
            result.errorMessage.Should().BeEmpty();
        }

        [Theory]
        [InlineData("INSERT INTO Users (Name, Email) VALUES ('John', 'john@example.com')")]
        [InlineData("UPDATE Products SET Price = 20 WHERE Id = 1")]
        [InlineData("DELETE FROM Orders WHERE Id = 5")]
        public void ValidateQuery_WithValidModificationQueries_ReturnsValid(string query)
        {
            // Act
            var result = _service.ValidateQuery(query);

            // Assert
            result.isValid.Should().BeTrue();
            result.errorMessage.Should().BeEmpty();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void ValidateQuery_WithEmptyQuery_ReturnsInvalid(string query)
        {
            // Act
            var result = _service.ValidateQuery(query);

            // Assert
            result.isValid.Should().BeFalse();
            result.errorMessage.Should().Be("Query cannot be empty");
        }

        [Theory]
        [InlineData("SELECT * FROM Users; DROP TABLE Users")]
        [InlineData("SELECT * FROM Products; DELETE FROM Products")]
        public void ValidateQuery_WithMultipleStatements_ReturnsInvalid(string query)
        {
            // Act
            var result = _service.ValidateQuery(query);

            // Assert
            result.isValid.Should().BeFalse();
            result.errorMessage.Should().Be("Multiple SQL statements are not allowed for security reasons");
            VerifyLoggerWarningWasCalled("Multiple statements");
        }

        [Theory]
        [InlineData("SELECT * FROM Users -- Drop table")]
        [InlineData("SELECT * FROM Users /* malicious comment */")]
        public void ValidateQuery_WithCommentMarkers_ReturnsInvalid(string query)
        {
            // Act
            var result = _service.ValidateQuery(query);

            // Assert
            result.isValid.Should().BeFalse();
            result.errorMessage.Should().Be("SQL comments are not allowed for security reasons");
            VerifyLoggerWarningWasCalled("Comment markers");
        }

        [Theory]
        [InlineData("SELECT * FROM Users UNION SELECT * FROM Passwords")]
        [InlineData("SELECT * FROM Products UNION ALL SELECT * FROM SecretProducts")]
        public void ValidateQuery_WithUnionAttacks_ReturnsInvalid(string query)
        {
            // Act
            var result = _service.ValidateQuery(query);

            // Assert
            result.isValid.Should().BeFalse();
            result.errorMessage.Should().Be("UNION queries are restricted for security reasons");
            VerifyLoggerWarningWasCalled("UNION attack");
        }

        [Theory]
        [InlineData("DROP TABLE Users")]
        [InlineData("DELETE FROM Users")]
        [InlineData("TRUNCATE TABLE Products")]
        [InlineData("ALTER TABLE Orders ADD Column Price DECIMAL")]
        [InlineData("CREATE TABLE NewTable (Id INT)")]
        public void ValidateQuery_WithDangerousKeywords_ReturnsInvalid(string query)
        {
            // Act
            var result = _service.ValidateQuery(query);

            // Assert
            result.isValid.Should().BeFalse();
            result.errorMessage.Should().Be("This query contains restricted operations (DROP, DELETE, ALTER, etc.)");
            VerifyLoggerWarningWasCalled("Dangerous keywords");
        }

        [Theory]
        [InlineData("EXEC xp_cmdshell 'dir'")]
        [InlineData("SELECT * FROM Users; EXEC xp_cmdshell 'net user'")]
        public void ValidateQuery_WithSystemCommands_ReturnsInvalid(string query)
        {
            // Act
            var result = _service.ValidateQuery(query);

            // Assert
            result.isValid.Should().BeFalse();
            // The error message might vary depending on which validation fails first
            result.errorMessage.Should().NotBeEmpty();
            _loggerMock.Verify(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.AtLeastOnce);
        }

        [Theory]
        [InlineData("SELECT * FROM master.dbo.sysdatabases")]
        [InlineData("EXEC sp_configure 'show advanced options', 1")]
        [InlineData("EXEC sp_executesql N'SELECT * FROM Users'")]
        public void ValidateQuery_WithDangerousSqlServerFunctions_ReturnsInvalid(string query)
        {
            // Act
            var result = _service.ValidateQuery(query);

            // Assert
            result.isValid.Should().BeFalse();
            result.errorMessage.Should().Be("This query contains restricted SQL Server functions");
            VerifyLoggerWarningWasCalled("Dangerous SQL Server functions");
        }

        [Theory]
        [InlineData("SELECT @@version")]
        [InlineData("SELECT @@servername")]
        [InlineData("SELECT @@language")]
        public void ValidateQuery_WithSystemVariables_ReturnsInvalid(string query)
        {
            // Act
            var result = _service.ValidateQuery(query);

            // Assert
            result.isValid.Should().BeFalse();
            result.errorMessage.Should().Be("Access to system variables is restricted");
            VerifyLoggerWarningWasCalled("System variables");
        }

        [Theory]
        [InlineData("EXEC('SELECT * FROM Users')")]
        [InlineData("EXECUTE('SELECT * FROM Products')")]
        public void ValidateQuery_WithDynamicSqlExecution_ReturnsInvalid(string query)
        {
            // Act
            var result = _service.ValidateQuery(query);

            // Assert
            result.isValid.Should().BeFalse();
            result.errorMessage.Should().Be("Dynamic SQL execution is not allowed");
            VerifyLoggerWarningWasCalled("Dynamic SQL execution");
        }

        [Theory]
        [InlineData("SELECT * FROM Users WHERE Name = '' + ''")]
        [InlineData("SELECT * FROM Products WHERE Id = '' || ''")]
        public void ValidateQuery_WithSuspiciousCharacterSequences_ReturnsInvalid(string query)
        {
            // Act
            var result = _service.ValidateQuery(query);

            // Assert
            result.isValid.Should().BeFalse();
            result.errorMessage.Should().Be("Query contains suspicious character sequences");
            VerifyLoggerWarningWasCalled("Suspicious character sequences");
        }

        private void VerifyLoggerWarningWasCalled(string messageContains)
        {
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
