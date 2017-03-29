namespace TanjiCore.Intercept.Crypto
{
    public struct RSAKeys
    {
        /// <summary>
        /// Gets the public modulus in hexadecimal.
        /// </summary>
        public string Modulus { get; }
        /// <summary>
        /// Gets the public exponent in hexadecimal.
        /// </summary>
        public string Exponent { get; }
        /// <summary>
        /// Gets the private exponent in hexadecimal.
        /// </summary>
        public string PrivateExponent { get; }

        /// <summary>
        /// Creates a <see cref="RSAKeys"/> structure that contains the specified public keys in hexadecimal.
        /// </summary>
        /// <param name="exponent"></param>
        /// <param name="modulus"></param>
        public RSAKeys(string exponent, string modulus)
            : this(exponent, modulus, string.Empty)
        { }
        /// <summary>
        /// Creates a <see cref="RSAKeys"/> structure that contains the specified public, and private keys in hexadecimal.
        /// </summary>
        /// <param name="exponent"></param>
        /// <param name="modulus"></param>
        /// <param name="privateExponent"></param>
        public RSAKeys(string exponent, string modulus, string privateExponent)
        {
            Modulus = modulus;
            Exponent = exponent;
            PrivateExponent = privateExponent;
        }

        public static bool operator ==(RSAKeys leftKeys, RSAKeys rightKeys)
        {
            return (leftKeys.Modulus.Equals(rightKeys.Modulus) &&
                leftKeys.Exponent.Equals(rightKeys.Exponent) &&
                leftKeys.PrivateExponent.Equals(rightKeys.PrivateExponent));
        }
        public static bool operator !=(RSAKeys leftKeys, RSAKeys rightKeys)
        {
            return !(leftKeys == rightKeys);
        }

        public bool Equals(RSAKeys keys)
        {
            return (this == keys);
        }
        /// <summary>
        /// Returns a value that indicates whether the current structure contains the same public key values as the compared structure.
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public bool EqualsPublic(RSAKeys keys)
        {
            return (Modulus.Equals(keys.Modulus) &&
                Exponent.Equals(keys.Exponent));
        }

        public override int GetHashCode()
        {
            return (Modulus.GetHashCode() ^
                Exponent.GetHashCode() ^
                PrivateExponent.GetHashCode());
        }
        public override bool Equals(object obj)
        {
            if (!(obj is RSAKeys))
            {
                return false;
            }
            return Equals((RSAKeys)obj);
        }
    }
}