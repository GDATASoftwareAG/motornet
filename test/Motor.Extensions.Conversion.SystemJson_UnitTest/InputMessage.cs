namespace Motor.Extensions.Conversion.SystemJson_UnitTest
{
    public class InputMessage
    {
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public int Age { get; set; }

        protected bool Equals(InputMessage other)
        {
            return string.Equals(Firstname, other.Firstname) && string.Equals(Lastname, other.Lastname) &&
                   Age == other.Age;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((InputMessage) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Firstname != null ? Firstname.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (Lastname != null ? Lastname.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Age;
                return hashCode;
            }
        }
    }
}
