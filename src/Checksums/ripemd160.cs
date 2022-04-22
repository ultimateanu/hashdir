using System.Security.Cryptography;


namespace Checksums
{
    using System;
    [System.Runtime.InteropServices.ComVisible(true)]
    public abstract class RIPEMD160 : HashAlgorithm
    {
        //
        // public constructors
        //

        protected RIPEMD160()
        {
            HashSizeValue = 160;
        }

        //
        // public methods
        //

        new static public RIPEMD160 Create()
        {
            return Create("System.Security.Cryptography.RIPEMD160");
        }

        // TODO: cleanup
        new static public RIPEMD160 Create(String hashName)
        {
            return (RIPEMD160)CryptoConfig.CreateFromName(hashName);
        }
    }
}
