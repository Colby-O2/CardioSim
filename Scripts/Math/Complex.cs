//-----------------------------------------------------------------------
// Author:  Colby-O
// File:    Complex.cs
//-----------------------------------------------------------------------
namespace ColbyO.CardioSim.Math
{
    /// <summary>
    /// Polar repersentation of a complex number (real + img * i).
    /// Implements simple arithmetic operations between complex numbers (+. -. *, and scaling)
    /// 
    /// </summary>
    internal struct Complex
    {
        public float real;
        public float imag;
        public Complex(float r, float i) { real = r; imag = i; }
        public static Complex operator +(Complex a, Complex b) => new Complex(a.real + b.real, a.imag + b.imag);
        public static Complex operator -(Complex a, Complex b) => new Complex(a.real - b.real, a.imag - b.imag);
        public static Complex operator *(Complex a, Complex b) => new Complex(a.real * b.real - a.imag * b.imag, a.real * b.imag + a.imag * b.real);
        public static Complex operator *(Complex a, float b) => new Complex(a.real * b, a.imag * b);
    }
}
