using System;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Exception thrown when a transaction feature is not supported by the underlying database provider.
    /// </summary>
    /// <remarks>
    /// This exception is thrown when attempting to use transaction features that are not supported
    /// by the current database provider. Different database systems have varying levels of support
    /// for transaction features:
    /// 
    /// <para><strong>Feature Support Matrix:</strong></para>
    /// <list type="table">
    /// <listheader>
    /// <term>Database</term>
    /// <description>Supported Features</description>
    /// </listheader>
    /// <item>
    /// <term>SQL Server</term>
    /// <description>All features (savepoints, nested savepoints, distributed transactions, all isolation levels)</description>
    /// </item>
    /// <item>
    /// <term>PostgreSQL</term>
    /// <description>All features (savepoints, nested savepoints, distributed transactions, all isolation levels)</description>
    /// </item>
    /// <item>
    /// <term>MySQL</term>
    /// <description>Limited savepoint support (no nested savepoints), all isolation levels</description>
    /// </item>
    /// <item>
    /// <term>SQLite</term>
    /// <description>No distributed transaction support, limited isolation level support</description>
    /// </item>
    /// <item>
    /// <term>Oracle</term>
    /// <description>All features (savepoints, nested savepoints, distributed transactions, all isolation levels)</description>
    /// </item>
    /// </list>
    /// 
    /// <para><strong>Common Scenarios:</strong></para>
    /// <list type="bullet">
    /// <item>Attempting to use distributed transactions with SQLite</item>
    /// <item>Attempting to create nested savepoints with MySQL</item>
    /// <item>Attempting to use Snapshot isolation level with databases that don't support it</item>
    /// </list>
    /// </remarks>
    public class UnsupportedDatabaseFeatureException : TransactionException
    {
        /// <summary>
        /// Gets the name of the database provider.
        /// </summary>
        public string? DatabaseProvider { get; }

        /// <summary>
        /// Gets the name of the unsupported feature.
        /// </summary>
        public string? FeatureName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsupportedDatabaseFeatureException"/> class.
        /// </summary>
        public UnsupportedDatabaseFeatureException()
            : base("The requested transaction feature is not supported by the database provider.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsupportedDatabaseFeatureException"/> class
        /// with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public UnsupportedDatabaseFeatureException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsupportedDatabaseFeatureException"/> class
        /// with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public UnsupportedDatabaseFeatureException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsupportedDatabaseFeatureException"/> class
        /// with database provider and feature information.
        /// </summary>
        /// <param name="databaseProvider">The name of the database provider.</param>
        /// <param name="featureName">The name of the unsupported feature.</param>
        public UnsupportedDatabaseFeatureException(string databaseProvider, string featureName)
            : base(BuildMessage(databaseProvider, featureName))
        {
            DatabaseProvider = databaseProvider;
            FeatureName = featureName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsupportedDatabaseFeatureException"/> class
        /// with database provider and feature information, and a reference to the inner exception.
        /// </summary>
        /// <param name="databaseProvider">The name of the database provider.</param>
        /// <param name="featureName">The name of the unsupported feature.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public UnsupportedDatabaseFeatureException(
            string databaseProvider, 
            string featureName, 
            Exception innerException)
            : base(BuildMessage(databaseProvider, featureName), innerException)
        {
            DatabaseProvider = databaseProvider;
            FeatureName = featureName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsupportedDatabaseFeatureException"/> class
        /// with a custom message, database provider and feature information.
        /// </summary>
        /// <param name="message">The custom error message.</param>
        /// <param name="databaseProvider">The name of the database provider.</param>
        /// <param name="featureName">The name of the unsupported feature.</param>
        public UnsupportedDatabaseFeatureException(
            string message,
            string databaseProvider,
            string featureName)
            : base(message)
        {
            DatabaseProvider = databaseProvider;
            FeatureName = featureName;
        }

        private static string BuildMessage(string databaseProvider, string featureName)
        {
            return $"The database provider '{databaseProvider}' does not support the '{featureName}' feature. " +
                   "Please consult the database compatibility matrix in the documentation for supported features.";
        }
    }
}
