using System.Runtime.Serialization;

namespace Paperless.REST.API.Models.BaseResponse
{
    /// <summary>
    /// Response for Diagnostics information.
    /// </summary>
    [DataContract]
    public class DiagnosticsResponse : IEquatable<DiagnosticsResponse>
    {
        /// <summary>
        /// Version of the application
        /// </summary>
        [DataMember]
        public string ApplicationVersion { get; set; } = null!;

        /// <summary>
        /// Version of the database
        /// </summary>
        [DataMember]
        public string DatabaseVersion { get; set; } = null!;

        /// <summary>
        /// Number of items in the processing queue
        /// </summary>
        [DataMember]
        public int QueueBacklog { get; set; }

        /// <summary>
        /// True if all workers are connected
        /// </summary>
        [DataMember]
        public bool WorkersConnected { get; set; }
        
        /// <summary>
        /// Determines whether the specified object is equal to the current instance.
        /// </summary>
        /// <remarks>This method performs a type check and delegates to the type-specific equality logic
        /// of the <see cref="DiagnosticsResponse"/> class.</remarks>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><see langword="true"/> if the specified object is of the same type and has the same value as the current
        /// instance;  otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((DiagnosticsResponse)obj);
        }

        /// <summary>
        /// Determines whether the specified <see cref="DiagnosticsResponse"/> instance is equal to the current
        /// instance. 
        /// </summary>
        /// <remarks>Two <see cref="DiagnosticsResponse"/> instances are considered equal if their
        /// <c>ApplicationVersion</c>, <c>DatabaseVersion</c>, <c>QueueBacklog</c>, and <c>WorkersConnected</c>
        /// properties are equal.</remarks>
        /// <param name="other">The <see cref="DiagnosticsResponse"/> instance to compare with the current instance.</param>
        /// <returns><see langword="true"/> if the specified <see cref="DiagnosticsResponse"/> instance is equal to the current
        /// instance; otherwise, <see langword="false"/>.</returns>
        public bool Equals(DiagnosticsResponse? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return (
                ApplicationVersion == other.ApplicationVersion ||
                ApplicationVersion != null &&
                ApplicationVersion.Equals(other.ApplicationVersion)
                ) && (
                DatabaseVersion == other.DatabaseVersion ||
                DatabaseVersion != null &&
                DatabaseVersion.Equals(other.DatabaseVersion)
                ) && (
                QueueBacklog == other.QueueBacklog ||
                QueueBacklog.Equals(other.QueueBacklog)
                ) && (
                WorkersConnected == other.WorkersConnected ||
                WorkersConnected.Equals(other.WorkersConnected)
                );
        }

        /// <summary>
        /// Computes a hash code for the current object based on its properties.
        /// </summary>
        /// <remarks>The hash code is calculated using the values of the <see cref="ApplicationVersion"/>,
        /// <see cref="DatabaseVersion"/>, <see cref="QueueBacklog"/>, and <see cref="WorkersConnected"/> properties. 
        /// This ensures that objects with the same property values produce the same hash code.</remarks>
        /// <returns>An integer representing the hash code of the current object.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 41;
                if (ApplicationVersion != null)
                    hashCode = hashCode * 59 + ApplicationVersion.GetHashCode();
                if (DatabaseVersion != null)
                    hashCode = hashCode * 59 + DatabaseVersion.GetHashCode();
                hashCode = hashCode * 59 + QueueBacklog.GetHashCode();
                hashCode = hashCode * 59 + WorkersConnected.GetHashCode();
                return hashCode;
            }
        }

    }
}
