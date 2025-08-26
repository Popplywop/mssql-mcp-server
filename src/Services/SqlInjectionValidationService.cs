using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Services
{
    public partial class SqlInjectionValidationService
    {
        private readonly ILogger<SqlInjectionValidationService> _logger;
        
        // Regex patterns for detecting common SQL injection attempts
        [GeneratedRegex(@";\s*\w", RegexOptions.IgnoreCase)]
        private static partial Regex MultipleStatementsPattern();
        
        [GeneratedRegex(@"--|\/\*")]
        private static partial Regex CommentPattern();
        
        [GeneratedRegex(@"\bunion\s+all\s+select\b|\bunion\s+select\b", RegexOptions.IgnoreCase)]
        private static partial Regex UnionPattern();
        
        [GeneratedRegex(@"\b(drop|truncate|alter|create)\b\s+\b(table|database|view|procedure|trigger|index|function)\b|\bdelete\s+from\s+\w+\s*$", RegexOptions.IgnoreCase)]
        private static partial Regex DangerousKeywordsPattern();
        
        [GeneratedRegex(@"\bxp_cmdshell\b", RegexOptions.IgnoreCase)]
        private static partial Regex XpCmdShellPattern();
        
        [GeneratedRegex(@"\b(master\.dbo|msdb\.dbo|xp_cmdshell|sp_configure|sp_executesql|sp_addlogin|sp_addsrvrolemember|sp_password)\b", RegexOptions.IgnoreCase)]
        private static partial Regex SqlServerFunctionPattern();
        
        [GeneratedRegex(@"@@(version|servername|language)", RegexOptions.IgnoreCase)]
        private static partial Regex SqlServerVariablePattern();
        
        [GeneratedRegex(@"\bexec\s*\(|execute\s*\(", RegexOptions.IgnoreCase)]
        private static partial Regex BatchExecutionPattern();
        
        [GeneratedRegex(@"['""]\s*\+\s*['""]\s*|['""]\s*\|\|\s*['""]\s*", RegexOptions.IgnoreCase)]
        private static partial Regex SqlInjectionCharsPattern();
        
        public SqlInjectionValidationService(ILogger<SqlInjectionValidationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Validates a SQL query for potential SQL injection attacks
        /// </summary>
        /// <param name="query">The SQL query to validate</param>
        /// <returns>A tuple containing (isValid, errorMessage)</returns>
        public (bool isValid, string errorMessage) ValidateQuery(string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return (false, "Query cannot be empty");
            }
            
            // Check for multiple statements (e.g., query1; query2)
            if (MultipleStatementsPattern().IsMatch(query))
            {
                _logger.LogWarning("SQL injection attempt detected: Multiple statements");
                return (false, "Multiple SQL statements are not allowed for security reasons");
            }
            
            // Check for comment markers that might be used to bypass validation
            if (CommentPattern().IsMatch(query))
            {
                _logger.LogWarning("SQL injection attempt detected: Comment markers");
                return (false, "SQL comments are not allowed for security reasons");
            }
            
            // Check for UNION-based attacks
            if (UnionPattern().IsMatch(query))
            {
                _logger.LogWarning("SQL injection attempt detected: UNION attack");
                return (false, "UNION queries are restricted for security reasons");
            }
            
            // Check for dangerous operations like DROP, DELETE, etc.
            if (DangerousKeywordsPattern().IsMatch(query))
            {
                _logger.LogWarning("SQL injection attempt detected: Dangerous keywords");
                return (false, "This query contains restricted operations (DROP, DELETE, ALTER, etc.)");
            }
            
            // Check for xp_cmdshell which can execute system commands
            if (XpCmdShellPattern().IsMatch(query))
            {
                _logger.LogWarning("SQL injection attempt detected: xp_cmdshell");
                return (false, "System command execution is not allowed");
            }
            
            // Check for dangerous SQL Server functions
            if (SqlServerFunctionPattern().IsMatch(query))
            {
                _logger.LogWarning("SQL injection attempt detected: Dangerous SQL Server functions");
                return (false, "This query contains restricted SQL Server functions");
            }
            
            // Check for SQL Server system variables
            if (SqlServerVariablePattern().IsMatch(query))
            {
                _logger.LogWarning("SQL injection attempt detected: System variables");
                return (false, "Access to system variables is restricted");
            }
            
            // Check for dynamic SQL execution
            if (BatchExecutionPattern().IsMatch(query))
            {
                _logger.LogWarning("SQL injection attempt detected: Dynamic SQL execution");
                return (false, "Dynamic SQL execution is not allowed");
            }
            
            // Check for SQL injection character sequences
            if (SqlInjectionCharsPattern().IsMatch(query))
            {
                _logger.LogWarning("SQL injection attempt detected: Suspicious character sequences");
                return (false, "Query contains suspicious character sequences");
            }
            
            // If all checks pass, the query is considered valid
            return (true, string.Empty);
        }
        
    }
}
