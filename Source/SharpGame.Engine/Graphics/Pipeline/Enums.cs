using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{

    public enum PolygonMode
    {
        Fill = 0,
        Line = 1,
        Point = 2,
        FillRectangleNV = 1000153000
    }

    public enum CullMode
    {
        None = 0,
        Front = 1,
        Back = 2,
        FrontAndBack = 3
    }

    public enum FrontFace
    {
        CounterClockwise = 0,
        Clockwise = 1
    }

    public enum SampleCountFlags
    {
        None = 0,
        Count1 = 1,
        Count2 = 2,
        Count4 = 4,
        Count8 = 8,
        Count16 = 16,
        Count32 = 32,
        Count64 = 64
    }

    public enum CompareOp
    {
        Never = 0,
        Less = 1,
        Equal = 2,
        LessOrEqual = 3,
        Greater = 4,
        NotEqual = 5,
        GreaterOrEqual = 6,
        Always = 7
    }

    public enum StencilOp
    {
        Keep = 0,
        Zero = 1,
        Replace = 2,
        IncrementAndClamp = 3,
        DecrementAndClamp = 4,
        Invert = 5,
        IncrementAndWrap = 6,
        DecrementAndWrap = 7
    }

    public enum BlendFactor
    {
        Zero = 0,
        One = 1,
        SrcColor = 2,
        OneMinusSrcColor = 3,
        DstColor = 4,
        OneMinusDstColor = 5,
        SrcAlpha = 6,
        OneMinusSrcAlpha = 7,
        DstAlpha = 8,
        OneMinusDstAlpha = 9,
        ConstantColor = 10,
        OneMinusConstantColor = 11,
        ConstantAlpha = 12,
        OneMinusConstantAlpha = 13,
        SrcAlphaSaturate = 14,
        Src1Color = 15,
        OneMinusSrc1Color = 16,
        Src1Alpha = 17,
        OneMinusSrc1Alpha = 18
    }

    public enum BlendOp
    {
        Add = 0,
        Subtract = 1,
        ReverseSubtract = 2,
        Min = 3,
        Max = 4,
        ZeroEXT = 1000148000,
        SrcEXT = 1000148001,
        DstEXT = 1000148002,
        SrcOverEXT = 1000148003,
        DstOverEXT = 1000148004,
        SrcInEXT = 1000148005,
        DstInEXT = 1000148006,
        SrcOutEXT = 1000148007,
        DstOutEXT = 1000148008,
        SrcAtopEXT = 1000148009,
        DstAtopEXT = 1000148010,
        XorEXT = 1000148011,
        MultiplyEXT = 1000148012,
        ScreenEXT = 1000148013,
        OverlayEXT = 1000148014,
        DarkenEXT = 1000148015,
        LightenEXT = 1000148016,
        ColordodgeEXT = 1000148017,
        ColorburnEXT = 1000148018,
        HardlightEXT = 1000148019,
        SoftlightEXT = 1000148020,
        DifferenceEXT = 1000148021,
        ExclusionEXT = 1000148022,
        InvertEXT = 1000148023,
        InvertRgbEXT = 1000148024,
        LineardodgeEXT = 1000148025,
        LinearburnEXT = 1000148026,
        VividlightEXT = 1000148027,
        LinearlightEXT = 1000148028,
        PinlightEXT = 1000148029,
        HardmixEXT = 1000148030,
        HslHueEXT = 1000148031,
        HslSaturationEXT = 1000148032,
        HslColorEXT = 1000148033,
        HslLuminosityEXT = 1000148034,
        PlusEXT = 1000148035,
        PlusClampedEXT = 1000148036,
        PlusClampedAlphaEXT = 1000148037,
        PlusDarkerEXT = 1000148038,
        MinusEXT = 1000148039,
        MinusClampedEXT = 1000148040,
        ContrastEXT = 1000148041,
        InvertOvgEXT = 1000148042,
        RedEXT = 1000148043,
        GreenEXT = 1000148044,
        BlueEXT = 1000148045
    }

    public enum ColorComponentFlags
    {
        None = 0,
        R = 1,
        G = 2,
        B = 4,
        A = 8,
        All = 0xf
    }

    public enum BlendMode
    {
        Replace = 0,
        Add,
        Multiply,
        Alpha,
        AddAlpha,
        PremulAlpha,
        InvdestAlpha,
        Subtract,
        SubtractAlpha,
    }


    public enum DynamicState
    {
        Viewport = 0,
        Scissor = 1,
        LineWidth = 2,
        DepthBias = 3,
        BlendConstants = 4,
        DepthBounds = 5,
        StencilCompareMask = 6,
        StencilWriteMask = 7,
        StencilReference = 8,
        ViewportWScalingNV = 1000087000,
        DiscardRectangleEXT = 1000099000,
        SampleLocationsEXT = 1000143000
    }

}
