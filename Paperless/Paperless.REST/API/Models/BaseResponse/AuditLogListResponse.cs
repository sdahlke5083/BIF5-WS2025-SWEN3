using System.Runtime.Serialization;

namespace Paperless.REST.API.Models.BaseResponse
{
    /// <summary>
    /// Respone for Audit Log.
    /// </summary>
    [DataContract]
    public class AuditLogListResponse : IEquatable<AuditLogListResponse>
    {
        [DataMember]
        public List<string> AuditLogs { get; set; }


        /// <summary>
        /// Determines whether the specified object is equal to the current instance.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><see langword="true"/> if the specified object is equal to the current instance; otherwise, <see
        /// langword="false"/>.</returns>
        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((AuditLogListResponse)obj);
        }


        /// <summary>
        /// Determines whether the current <see cref="AuditLogListResponse"/> instance is equal to another specified
        /// <see cref="AuditLogListResponse"/> instance.
        /// </summary>
        /// <remarks>Two <see cref="AuditLogListResponse"/> instances are considered equal if their
        /// <c>AuditLogs</c> collections are either both <see langword="null"/>  or contain the same elements in the
        /// same order.</remarks>
        /// <param name="other">The <see cref="AuditLogListResponse"/> instance to compare with the current instance.</param>
        /// <returns><see langword="true"/> if the specified <see cref="AuditLogListResponse"/> is equal to the current instance;
        /// otherwise, <see langword="false"/>.</returns>
        public bool Equals(AuditLogListResponse? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return (AuditLogs == other.AuditLogs || (AuditLogs != null && other.AuditLogs != null && AuditLogs.SequenceEqual(other.AuditLogs)));
        }

        /// <summary>
        /// Returns a hash code for the current object.
        /// </summary>
        /// <remarks>The hash code is computed based on the type of the object and the hash code of the
        /// <see cref="AuditLogs"/> property, if it is not null.</remarks>
        /// <returns>An integer representing the hash code for the current object.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = GetType().GetHashCode();
                if (AuditLogs != null)
                    hashCode = (hashCode * 397) ^ AuditLogs.GetHashCode();
                return hashCode;
            }

        }
    }
}
