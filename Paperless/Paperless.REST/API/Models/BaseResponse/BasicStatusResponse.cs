namespace Paperless.REST.API.Models.BaseResponse
{
    /// <summary>
    /// Represents a basic status response containing a human readable status message.
    /// </summary>
    /// <remarks>This class is used to encapsulate a simple result of an operation, providing a
    /// human-readable status message.</remarks>
    public class BasicStatusResponse : IEquatable<BasicStatusResponse>
    {
        /// <summary>
        /// Human-readable status message
        /// </summary>
        public string Status { get; set; } = null!;

        /// <summary>
        /// Returns true if objects are equal.
        /// </summary>
        /// <param name="obj">Object to be compared to.</param>
        /// <returns><see cref="bool"/></returns>
        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((BasicStatusResponse)obj);
        }


        /// <summary>
        /// Returns true if this instance is equal to another instance of BasicStatusResponse.
        /// </summary>
        /// <param name="other">BasicStatusResponse to be compared</param>
        /// <returns><see cref="bool"/></returns>
        public bool Equals(BasicStatusResponse? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return (
                Status == other.Status ||
                Status != null &&
                Status.Equals(other.Status)
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>The Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hashCode = 41;
                if (Status != null)
                    hashCode = hashCode * 59 + Status.GetHashCode();
                return hashCode;
            }
        }

        #region Operators
        #pragma warning disable 1591

        public static bool operator ==(BasicStatusResponse left, BasicStatusResponse right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(BasicStatusResponse left, BasicStatusResponse right)
        {
            return !Equals(left, right);
        }

        #pragma warning restore 1591
        #endregion Operators
    }
}
