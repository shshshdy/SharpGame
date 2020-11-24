﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public enum PrimitiveTopology
    {
        PointList = 0,
        LineList = 1,
        LineStrip = 2,
        TriangleList = 3,
        TriangleStrip = 4,
        TriangleFan = 5,
        LineListWithAdjacency = 6,
        LineStripWithAdjacency = 7,
        TriangleListWithAdjacency = 8,
        TriangleStripWithAdjacency = 9,
        PatchList = 10
    }

    public enum IndexType
    {
        Uint16 = 0,
        Uint32 = 1
    }

    public enum Format
    {
        Undefined = 0,
        R4g4UnormPack8 = 1,
        R4g4b4a4UnormPack16 = 2,
        B4g4r4a4UnormPack16 = 3,
        R5g6b5UnormPack16 = 4,
        B5g6r5UnormPack16 = 5,
        R5g5b5a1UnormPack16 = 6,
        B5g5r5a1UnormPack16 = 7,
        A1r5g5b5UnormPack16 = 8,
        R8Unorm = 9,
        R8Snorm = 10,
        R8Uscaled = 11,
        R8Sscaled = 12,
        R8Uint = 13,
        R8Sint = 14,
        R8Srgb = 15,
        R8g8Unorm = 16,
        R8g8Snorm = 17,
        R8g8Uscaled = 18,
        R8g8Sscaled = 19,
        R8g8Uint = 20,
        R8g8Sint = 21,
        R8g8Srgb = 22,
        R8g8b8Unorm = 23,
        R8g8b8Snorm = 24,
        R8g8b8Uscaled = 25,
        R8g8b8Sscaled = 26,
        R8g8b8Uint = 27,
        R8g8b8Sint = 28,
        R8g8b8Srgb = 29,
        B8g8r8Unorm = 30,
        B8g8r8Snorm = 31,
        B8g8r8Uscaled = 32,
        B8g8r8Sscaled = 33,
        B8g8r8Uint = 34,
        B8g8r8Sint = 35,
        B8g8r8Srgb = 36,
        R8g8b8a8Unorm = 37,
        R8g8b8a8Snorm = 38,
        R8g8b8a8Uscaled = 39,
        R8g8b8a8Sscaled = 40,
        R8g8b8a8Uint = 41,
        R8g8b8a8Sint = 42,
        R8g8b8a8Srgb = 43,
        B8g8r8a8Unorm = 44,
        B8g8r8a8Snorm = 45,
        B8g8r8a8Uscaled = 46,
        B8g8r8a8Sscaled = 47,
        B8g8r8a8Uint = 48,
        B8g8r8a8Sint = 49,
        B8g8r8a8Srgb = 50,
        A8b8g8r8UnormPack32 = 51,
        A8b8g8r8SnormPack32 = 52,
        A8b8g8r8UscaledPack32 = 53,
        A8b8g8r8SscaledPack32 = 54,
        A8b8g8r8UintPack32 = 55,
        A8b8g8r8SintPack32 = 56,
        A8b8g8r8SrgbPack32 = 57,
        A2r10g10b10UnormPack32 = 58,
        A2r10g10b10SnormPack32 = 59,
        A2r10g10b10UscaledPack32 = 60,
        A2r10g10b10SscaledPack32 = 61,
        A2r10g10b10UintPack32 = 62,
        A2r10g10b10SintPack32 = 63,
        A2b10g10r10UnormPack32 = 64,
        A2b10g10r10SnormPack32 = 65,
        A2b10g10r10UscaledPack32 = 66,
        A2b10g10r10SscaledPack32 = 67,
        A2b10g10r10UintPack32 = 68,
        A2b10g10r10SintPack32 = 69,
        R16Unorm = 70,
        R16Snorm = 71,
        R16Uscaled = 72,
        R16Sscaled = 73,
        R16Uint = 74,
        R16Sint = 75,
        R16Sfloat = 76,
        R16g16Unorm = 77,
        R16g16Snorm = 78,
        R16g16Uscaled = 79,
        R16g16Sscaled = 80,
        R16g16Uint = 81,
        R16g16Sint = 82,
        R16g16Sfloat = 83,
        R16g16b16Unorm = 84,
        R16g16b16Snorm = 85,
        R16g16b16Uscaled = 86,
        R16g16b16Sscaled = 87,
        R16g16b16Uint = 88,
        R16g16b16Sint = 89,
        R16g16b16Sfloat = 90,
        R16g16b16a16Unorm = 91,
        R16g16b16a16Snorm = 92,
        R16g16b16a16Uscaled = 93,
        R16g16b16a16Sscaled = 94,
        R16g16b16a16Uint = 95,
        R16g16b16a16Sint = 96,
        R16g16b16a16Sfloat = 97,
        R32Uint = 98,
        R32Sint = 99,
        R32Sfloat = 100,
        R32g32Uint = 101,
        R32g32Sint = 102,
        R32g32Sfloat = 103,
        R32g32b32Uint = 104,
        R32g32b32Sint = 105,
        R32g32b32Sfloat = 106,
        R32g32b32a32Uint = 107,
        R32g32b32a32Sint = 108,
        R32g32b32a32Sfloat = 109,
        R64Uint = 110,
        R64Sint = 111,
        R64Sfloat = 112,
        R64g64Uint = 113,
        R64g64Sint = 114,
        R64g64Sfloat = 115,
        R64g64b64Uint = 116,
        R64g64b64Sint = 117,
        R64g64b64Sfloat = 118,
        R64g64b64a64Uint = 119,
        R64g64b64a64Sint = 120,
        R64g64b64a64Sfloat = 121,
        B10g11r11UfloatPack32 = 122,
        E5b9g9r9UfloatPack32 = 123,
        D16Unorm = 124,
        X8D24UnormPack32 = 125,
        D32Sfloat = 126,
        S8Uint = 127,
        D16UnormS8Uint = 128,
        D24UnormS8Uint = 129,
        D32SfloatS8Uint = 130,
        Bc1RgbUnormBlock = 131,
        Bc1RgbSrgbBlock = 132,
        Bc1RgbaUnormBlock = 133,
        Bc1RgbaSrgbBlock = 134,
        Bc2UnormBlock = 135,
        Bc2SrgbBlock = 136,
        Bc3UnormBlock = 137,
        Bc3SrgbBlock = 138,
        Bc4UnormBlock = 139,
        Bc4SnormBlock = 140,
        Bc5UnormBlock = 141,
        Bc5SnormBlock = 142,
        Bc6hUfloatBlock = 143,
        Bc6hSfloatBlock = 144,
        Bc7UnormBlock = 145,
        Bc7SrgbBlock = 146,
        Etc2R8g8b8UnormBlock = 147,
        Etc2R8g8b8SrgbBlock = 148,
        Etc2R8g8b8a1UnormBlock = 149,
        Etc2R8g8b8a1SrgbBlock = 150,
        Etc2R8g8b8a8UnormBlock = 151,
        Etc2R8g8b8a8SrgbBlock = 152,
        EacR11UnormBlock = 153,
        EacR11SnormBlock = 154,
        EacR11g11UnormBlock = 155,
        EacR11g11SnormBlock = 156,
        Astc4x4UnormBlock = 157,
        Astc4x4SrgbBlock = 158,
        Astc5x4UnormBlock = 159,
        Astc5x4SrgbBlock = 160,
        Astc5x5UnormBlock = 161,
        Astc5x5SrgbBlock = 162,
        Astc6x5UnormBlock = 163,
        Astc6x5SrgbBlock = 164,
        Astc6x6UnormBlock = 165,
        Astc6x6SrgbBlock = 166,
        Astc8x5UnormBlock = 167,
        Astc8x5SrgbBlock = 168,
        Astc8x6UnormBlock = 169,
        Astc8x6SrgbBlock = 170,
        Astc8x8UnormBlock = 171,
        Astc8x8SrgbBlock = 172,
        Astc10x5UnormBlock = 173,
        Astc10x5SrgbBlock = 174,
        Astc10x6UnormBlock = 175,
        Astc10x6SrgbBlock = 176,
        Astc10x8UnormBlock = 177,
        Astc10x8SrgbBlock = 178,
        Astc10x10UnormBlock = 179,
        Astc10x10SrgbBlock = 180,
        Astc12x10UnormBlock = 181,
        Astc12x10SrgbBlock = 182,
        Astc12x12UnormBlock = 183,
        Astc12x12SrgbBlock = 184,
        Pvrtc12bppUnormBlockImg = 1000054000,
        Pvrtc14bppUnormBlockImg = 1000054001,
        Pvrtc22bppUnormBlockImg = 1000054002,
        Pvrtc24bppUnormBlockImg = 1000054003,
        Pvrtc12bppSrgbBlockImg = 1000054004,
        Pvrtc14bppSrgbBlockImg = 1000054005,
        Pvrtc22bppSrgbBlockImg = 1000054006,
        Pvrtc24bppSrgbBlockImg = 1000054007,
        G8b8g8r8422UnormKHR = 1000156000,
        B8g8r8g8422UnormKHR = 1000156001,
        G8B8R83plane420UnormKHR = 1000156002,
        G8B8r82plane420UnormKHR = 1000156003,
        G8B8R83plane422UnormKHR = 1000156004,
        G8B8r82plane422UnormKHR = 1000156005,
        G8B8R83plane444UnormKHR = 1000156006,
        R10x6UnormPack16KHR = 1000156007,
        R10x6g10x6Unorm2pack16KHR = 1000156008,
        R10x6g10x6b10x6a10x6Unorm4pack16KHR = 1000156009,
        G10x6b10x6g10x6r10x6422Unorm4pack16KHR = 1000156010,
        B10x6g10x6r10x6g10x6422Unorm4pack16KHR = 1000156011,
        G10x6B10x6R10x63plane420Unorm3pack16KHR = 1000156012,
        G10x6B10x6r10x62plane420Unorm3pack16KHR = 1000156013,
        G10x6B10x6R10x63plane422Unorm3pack16KHR = 1000156014,
        G10x6B10x6r10x62plane422Unorm3pack16KHR = 1000156015,
        G10x6B10x6R10x63plane444Unorm3pack16KHR = 1000156016,
        R12x4UnormPack16KHR = 1000156017,
        R12x4g12x4Unorm2pack16KHR = 1000156018,
        R12x4g12x4b12x4a12x4Unorm4pack16KHR = 1000156019,
        G12x4b12x4g12x4r12x4422Unorm4pack16KHR = 1000156020,
        B12x4g12x4r12x4g12x4422Unorm4pack16KHR = 1000156021,
        G12x4B12x4R12x43plane420Unorm3pack16KHR = 1000156022,
        G12x4B12x4r12x42plane420Unorm3pack16KHR = 1000156023,
        G12x4B12x4R12x43plane422Unorm3pack16KHR = 1000156024,
        G12x4B12x4r12x42plane422Unorm3pack16KHR = 1000156025,
        G12x4B12x4R12x43plane444Unorm3pack16KHR = 1000156026,
        G16b16g16r16422UnormKHR = 1000156027,
        B16g16r16g16422UnormKHR = 1000156028,
        G16B16R163plane420UnormKHR = 1000156029,
        G16B16r162plane420UnormKHR = 1000156030,
        G16B16R163plane422UnormKHR = 1000156031,
        G16B16r162plane422UnormKHR = 1000156032,
        G16B16R163plane444UnormKHR = 1000156033
    }

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

    public enum ImageCreateFlags
    {
        None = 0,
        SparseBinding = 1,
        SparseResidency = 2,
        SparseAliased = 4,
        MutableFormat = 8,
        CubeCompatible = 16,
        _2dArrayCompatibleKHR = 32,
        BindSfrKHX = 64,
        BlockTexelViewCompatibleKHR = 128,
        ExtendedUsageKHR = 256,
        DisjointKHR = 512,
        AliasKHR = 1024,
        SampleLocationsCompatibleDepthEXT = 4096
    }

    public enum ImageType
    {
        Image1D = 0,
        Image2D = 1,
        Image3D = 2
    }

    public enum ImageUsageFlags
    {
        None = 0,
        TransferSrc = 1,
        TransferDst = 2,
        Sampled = 4,
        Storage = 8,
        ColorAttachment = 16,
        DepthStencilAttachment = 32,
        TransientAttachment = 64,
        InputAttachment = 128
    }

    public enum ImageLayout
    {
        Undefined = 0,
        General = 1,
        ColorAttachmentOptimal = 2,
        DepthStencilAttachmentOptimal = 3,
        DepthStencilReadOnlyOptimal = 4,
        ShaderReadOnlyOptimal = 5,
        TransferSrcOptimal = 6,
        TransferDstOptimal = 7,
        Preinitialized = 8,
        PresentSrcKHR = 1000001002,
        SharedPresentKHR = 1000111000,
        DepthReadOnlyStencilAttachmentOptimalKHR = 1000117000,
        DepthAttachmentStencilReadOnlyOptimalKHR = 1000117001
    }

    public enum ImageTiling
    {
        Optimal = 0,
        Linear = 1
    }

    public enum ImageViewType
    {
        Image1D = 0,
        Image2D = 1,
        Image3D = 2,
        ImageCube = 3,
        Image1DArray = 4,
        Image2DArray = 5,
        ImageCubeArray = 6
    }

    public enum ComponentSwizzle
    {
        Identity = 0,
        Zero = 1,
        One = 2,
        R = 3,
        G = 4,
        B = 5,
        A = 6
    }

    public enum BorderColor
    {
        FloatTransparentBlack = 0,
        IntTransparentBlack = 1,
        FloatOpaqueBlack = 2,
        IntOpaqueBlack = 3,
        FloatOpaqueWhite = 4,
        IntOpaqueWhite = 5
    }

    public enum SamplerAddressMode
    {
        Repeat = 0,
        MirroredRepeat = 1,
        ClampToEdge = 2,
        ClampToBorder = 3,
        MirrorClampToEdge = 4
    }

    public enum SamplerMipmapMode
    {
        Nearest = 0,
        Linear = 1
    }

    public enum Filter
    {
        Nearest = 0,
        Linear = 1,
        CubicImg = 1000015000
    }

}
