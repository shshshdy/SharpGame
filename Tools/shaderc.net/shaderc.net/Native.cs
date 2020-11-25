﻿// Copyright (c) 2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Runtime.InteropServices;

namespace shaderc {
	public enum TargetEnvironment : byte {
		/// <summary>
		/// SPIR-V under Vulkan semantics
		/// </summary>
		Vulkan,
		/// <summary>
		/// SPIR-V under OpenGL semantics
		/// </summary>
		/// <remarks>
		/// NOTE: SPIR-V code generation is not supported for shaders under OpenGL
		/// compatibility profile.
		/// </remarks>
		OpenGL,
		/// <summary>
		/// SPIR-V under OpenGL semantics,
		/// including compatibility profile
		/// functions
		/// </summary>
		OpenGLCompat,
		/// <summary>
		/// SPIR-V under WebGPU semantics.
		/// </summary>
		WebGPU,
		Default = Vulkan
	}

	/// <summary>
	/// Target environment version
	/// </summary>
	/// <remarks>
	/// For Vulkan, use Vulkan's mapping of version numbers to integers.
	/// See vulkan.h
	/// 
	/// For OpenGL, use the number from #version in shaders.
	/// TODO(dneto): Currently no difference between OpenGL 4.5 and 4.6.
	/// See glslang/Standalone/Standalone.cpp
	/// TODO(dneto): Glslang doesn't accept a OpenGL client version of 460.
	/// Currently WebGPU doesn't have versioning, since it isn't finalized. This
	/// will have to be updated once the spec is finished.
	/// </remarks>
	public enum EnvironmentVersion : UInt32 {
		Vulkan_1_0 = ((1u << 22)),
		Vulkan_1_1 = ((1u << 22) | (1 << 12)),
		Vulkan_1_2 = ((1u << 22) | (2 << 12)),
		OpenGL_4_5 = 450,
		WebGPU,
	}

	/// <summary>
	/// The known versions of SPIR-V.
	/// </summary>
	/// <remarks>
	/// Use the values used for word 1 of a SPIR-V binary:
	/// - bits 24 to 31: zero
	/// - bits 16 to 23: major version number
	/// - bits 8 to 15: minor version number
	/// - bits 0 to 7: zero
	/// </remarks>
	public struct SpirVVersion : IEquatable<SpirVVersion> {
		internal readonly UInt32 version;

		public uint Major => (version & 0xff0000) >> 16;
		public uint Minor => (version & 0xff00) >> 8;

		public bool Equals (SpirVVersion other) 
			=> version == other.version;
		public override int GetHashCode () =>
			version.GetHashCode ();
		public override bool Equals (object obj) =>
			obj is SpirVVersion sv &&  Equals (sv);
		public SpirVVersion (UInt32 version) {
			this.version = version;
		}
		public SpirVVersion (uint major, uint minor) {
			this.version = (major << 16) + (minor << 8);
		}
		public override string ToString () => $"SPIR-V {Major}.{Minor}";
	}

	public enum SourceLanguage : byte {
		Glsl,
		Hlsl,
	};
	// Indicate the status of a compilation.
	public enum Status : byte {
		Success = 0,
		InvalidStage = 1,  // error stage deduction
		CompilationError = 2,
		InternalError = 3,  // unexpected failure
		NullResultObject = 4,
		InvalidAssembly = 5,
		ValidationError = 6,
		TransformationError = 7,
		ConfigurationError = 8,
	}

	public enum ShaderKind : byte {
		VertexShader,
		FragmentShader,
		ComputeShader,
		GeometryShader,
		TessControlShader,
		TessEvaluationShader,
		GlslVertexShader = VertexShader,
		GlslFragmentShader = FragmentShader,
		GlslComputeShader = ComputeShader,
		GlslGeometryShader = GeometryShader,
		GlslTessControlShader = TessControlShader,
		GlslTessEvaluationShader = TessEvaluationShader,
		GlslInferFromSource,
		GlslDefaultVertexShader,
		GlslDefaultFragmentShader,
		GlslDefaultComputeShader,
		GlslDefaultGeometryShader,
		GlslDefaultTessControlShader,
		GlslDefaultTessEvaluationShader,
		SpirvAssembly,
		RaygenShader,
		AnyhitShader,
		ClosesthitShader,
		MissShader,
		IntersectionShader,
		CallableShader,
		GlslRaygenShader = RaygenShader,
		GlslAnyhitShader = AnyhitShader,
		GlslClosesthitShader = ClosesthitShader,
		GlslMissShader = MissShader,
		GlslIntersectionShader = IntersectionShader,
		GlslCallableShader = CallableShader,
		GlslDefaultRaygenShader,
		GlslDefaultAnyhitShader,
		GlslDefaultClosesthitShader,
		GlslDefaultMissShader,
		GlslDefaultIntersectionShader,
		GlslDefaultCallableShader,
		TaskShader,
		MeshShader,
		GlslTaskShader = TaskShader,
		GlslMeshShader = MeshShader,
		GlslDefaultTaskShader,
		GlslDefaultMeshShader,
	};
	public enum Profile : byte {
		None,
		Core,
		Compatibility,
		Es,
	};
	public enum OptimizationLevel : byte {
		Zero,
		Size,
		Performance,
	};
	public enum Limit : byte {
		MaxLights,
		MaxClipPlanes,
		MaxTextureUnits,
		MaxTextureCoords,
		MaxVertexAttribs,
		MaxVertexUniformComponents,
		MaxVaryingFloats,
		MaxVertexTextureImageUnits,
		MaxCombinedTextureImageUnits,
		MaxTextureImageUnits,
		MaxFragmentUniformComponents,
		MaxDrawBuffers,
		MaxVertexUniformVectors,
		MaxVaryingVectors,
		MaxFragmentUniformVectors,
		MaxVertexOutputVectors,
		MaxFragmentInputVectors,
		MinProgramTexelOffset,
		MaxProgramTexelOffset,
		MaxClipDistances,
		MaxComputeWorkGroupCountX,
		MaxComputeWorkGroupCountY,
		MaxComputeWorkGroupCountZ,
		MaxComputeWorkGroupSizeX,
		MaxComputeWorkGroupSizeY,
		MaxComputeWorkGroupSizeZ,
		MaxComputeUniformComponents,
		MaxComputeTextureImageUnits,
		MaxComputeImageUniforms,
		MaxComputeAtomicCounters,
		MaxComputeAtomicCounterBuffers,
		MaxVaryingComponents,
		MaxVertexOutputComponents,
		MaxGeometryInputComponents,
		MaxGeometryOutputComponents,
		MaxFragmentInputComponents,
		MaxImageUnits,
		MaxCombinedImageUnitsAndFragmentOutputs,
		MaxCombinedShaderOutputResources,
		MaxImageSamples,
		MaxVertexImageUniforms,
		MaxTessControlImageUniforms,
		MaxTessEvaluationImageUniforms,
		MaxGeometryImageUniforms,
		MaxFragmentImageUniforms,
		MaxCombinedImageUniforms,
		MaxGeometryTextureImageUnits,
		MaxGeometryOutputVertices,
		MaxGeometryTotalOutputComponents,
		MaxGeometryUniformComponents,
		MaxGeometryVaryingComponents,
		MaxTessControlInputComponents,
		MaxTessControlOutputComponents,
		MaxTessControlTextureImageUnits,
		MaxTessControlUniformComponents,
		MaxTessControlTotalOutputComponents,
		MaxTessEvaluationInputComponents,
		MaxTessEvaluationOutputComponents,
		MaxTessEvaluationTextureImageUnits,
		MaxTessEvaluationUniformComponents,
		MaxTessPatchComponents,
		MaxPatchVertices,
		MaxTessGenLevel,
		MaxViewports,
		MaxVertexAtomicCounters,
		MaxTessControlAtomicCounters,
		MaxTessEvaluationAtomicCounters,
		MaxGeometryAtomicCounters,
		MaxFragmentAtomicCounters,
		MaxCombinedAtomicCounters,
		MaxAtomicCounterBindings,
		MaxVertexAtomicCounterBuffers,
		MaxTessControlAtomicCounterBuffers,
		MaxTessEvaluationAtomicCounterBuffers,
		MaxGeometryAtomicCounterBuffers,
		MaxFragmentAtomicCounterBuffers,
		MaxCombinedAtomicCounterBuffers,
		MaxAtomicCounterBufferSize,
		MaxTransformFeedbackBuffers,
		MaxTransformFeedbackInterleavedComponents,
		MaxCullDistances,
		MaxCombinedClipAndCullDistances,
		MaxSamples,
	};
	public enum UniformKind : byte {
		Image,
		Sampler,
		Texture,
		Buffer,
		StorageBuffer,
		UnorderedAccessView,
	};

	/// <summary>
	/// The kinds of include requests.
	/// </summary>
	public enum IncludeType {
		/// <summary>
		/// E.g. #include "source"
		/// </summary>
		Relative,
		/// <summary>
		/// E.g. #include &lt;source>
		/// </summary>
		Standard
	};

	/// <summary>
	/// An include result.
	/// </summary>
	public struct IncludeResult {
		readonly IntPtr sourceName;
		readonly UIntPtr sourceNameLength;
		readonly IntPtr content;
		readonly UIntPtr contentLength;
		/// <summary>
		/// User data to be passed along with this request.
		/// </summary>
		public readonly IntPtr UserData;
		/// <summary>
		/// The name of the source file.  The name should be fully resolved
		/// in the sense that it should be a unique name in the context of the
		/// includer.  For example, if the includer maps source names to files in
		/// a filesystem, then this name should be the absolute path of the file.
		/// For a failed inclusion, this string is empty.
		/// </summary>
		public string SourceName => Marshal.PtrToStringAnsi (sourceName, (int)sourceNameLength);
		/// <summary>
		/// The text contents of the source file in the normal case.
		/// For a failed inclusion, this contains the error message.
		/// </summary>
		public string Content => Marshal.PtrToStringAnsi (content, (int)contentLength);

		internal IncludeResult (string sourceName, string content, int optionsId) {
			this.sourceName = Marshal.StringToHGlobalAnsi (sourceName);
			sourceNameLength = (UIntPtr)sourceName.Length;
			this.content = Marshal.StringToHGlobalAnsi (content);
			contentLength = (UIntPtr)content.Length;
			UserData = (IntPtr)optionsId;
		}
		internal void FreeStrings () {
			Marshal.FreeHGlobal (sourceName);
			Marshal.FreeHGlobal (content);
		}
	}

	// An includer callback type for mapping an #include request to an include
	// result.  The user_data parameter specifies the client context.  The
	// requested_source parameter specifies the name of the source being requested.
	// The type parameter specifies the kind of inclusion request being made.
	// The requesting_source parameter specifies the name of the source containing
	// the #include request.  The includer owns the result object and its contents,
	// and both must remain valid until the release callback is called on the result
	// object.
	internal delegate IntPtr PFN_IncludeResolve (IntPtr userData, string requestedSource, int type, string requestingSource, UIntPtr includeDepth);
	// An includer callback type for destroying an include result.
	internal delegate void PFN_IncludeResultRelease (IntPtr userData, IntPtr includeResult);

	internal static class NativeMethods {
		const string lib = "shaderc_shared";

		[DllImport (lib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr shaderc_compiler_initialize ();
		[DllImport (lib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void shaderc_compiler_release (IntPtr sh);
		[DllImport (lib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr shaderc_compile_options_initialize ();
		[DllImport (lib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void shaderc_compile_options_release (IntPtr options);
		[DllImport (lib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void shaderc_result_release (IntPtr options);

		[DllImport (lib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern ulong shaderc_result_get_length (IntPtr result);
		[DllImport (lib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern ulong shaderc_result_get_num_warnings (IntPtr result);
		[DllImport (lib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern ulong shaderc_result_get_num_errors (IntPtr result);
		[DllImport (lib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern Status shaderc_result_get_compilation_status (IntPtr result);
		[DllImport (lib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr shaderc_result_get_bytes (IntPtr result);
		[DllImport (lib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr shaderc_result_get_error_message (IntPtr result);

		[DllImport (lib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void shaderc_get_spv_version (out SpirVVersion version, out uint revision);

		// Parses the version and profile from a given null-terminated string
		// containing both version and profile, like: '450core'. Returns false if
		// the string can not be parsed. Returns true when the parsing succeeds. The
		// parsed version and profile are returned through arguments.
		[DllImport (lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		internal static extern bool shaderc_parse_version_profile (string str, out int version, out Profile profile);

		// Sets includer callback functions.
		[DllImport (lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void shaderc_compile_options_set_include_callbacks (IntPtr options, IntPtr resolver, IntPtr result_releaser, IntPtr user_data);


		[DllImport (lib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr shaderc_compile_options_clone (IntPtr options);
		// Adds a predefined macro to the compilation options. This has the same
		// effect as passing -Dname=value to the command-line compiler.  If value
		// is NULL, it has the same effect as passing -Dname to the command-line
		// compiler. If a macro definition with the same name has previously been
		// added, the value is replaced with the new value. The macro name and
		// value are passed in with char pointers, which point to their data, and
		// the lengths of their data. The strings that the name and value pointers
		// point to must remain valid for the duration of the call, but can be
		// modified or deleted after this function has returned. In case of adding
		// a valueless macro, the value argument should be a null pointer or the
		// value_length should be 0u.
		[DllImport (lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void shaderc_compile_options_add_macro_definition (IntPtr options, string name, ulong name_length, string value, ulong value_length);
		// Sets the source language.  The default is GLSL.
		[DllImport (lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void shaderc_compile_options_set_source_language (IntPtr options, SourceLanguage lang);
		// Sets the compiler mode to generate debug information in the output.
		[DllImport (lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void shaderc_compile_options_set_generate_debug_info (IntPtr options);
		// Sets the compiler optimization level to the given level. Only the last one
		// takes effect if multiple calls of this function exist.
		[DllImport (lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void shaderc_compile_options_set_optimization_level (IntPtr options, OptimizationLevel level);
		// Forces the GLSL language version and profile to a given pair. The version
		// number is the same as would appear in the #version annotation in the source.
		// Version and profile specified here overrides the #version annotation in the
		// source. Use profile: 'shaderc_profile_none' for GLSL versions that do not
		// define profiles, e.g. versions below 150.
		[DllImport (lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void shaderc_compile_options_set_forced_version_profile (IntPtr options, int version, Profile profile);

		// Sets the compiler mode to suppress warnings, overriding warnings-as-errors
		// mode. When both suppress-warnings and warnings-as-errors modes are
		// turned on, warning messages will be inhibited, and will not be emitted
		// as error messages.
		[DllImport (lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void shaderc_compile_options_set_suppress_warnings (IntPtr options);

		// Sets the target shader environment, affecting which warnings or errors will
		// be issued.  The version will be for distinguishing between different versions
		// of the target environment.  The version value should be either 0 or
		// a value listed in shaderc_env_version.  The 0 value maps to Vulkan 1.0 if
		// |target| is Vulkan, and it maps to OpenGL 4.5 if |target| is OpenGL.
		[DllImport (lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void shaderc_compile_options_set_target_env (IntPtr options,TargetEnvironment target, EnvironmentVersion version);

		// Sets the target SPIR-V version. The generated module will use this version
		// of SPIR-V.  Each target environment determines what versions of SPIR-V
		// it can consume.  Defaults to the highest version of SPIR-V 1.0 which is
		// required to be supported by the target environment.  E.g. Default to SPIR-V
		// 1.0 for Vulkan 1.0 and SPIR-V 1.3 for Vulkan 1.1.
		[DllImport (lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void shaderc_compile_options_set_target_spirv (IntPtr options, SpirVVersion version);

		// Sets the compiler mode to treat all warnings as errors. Note the
		// suppress-warnings mode overrides this option, i.e. if both
		// warning-as-errors and suppress-warnings modes are set, warnings will not
		// be emitted as error messages.
		[DllImport (lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void shaderc_compile_options_set_warnings_as_errors (IntPtr options);

		// Sets a resource limit.
		[DllImport (lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void shaderc_compile_options_set_limit (IntPtr options, Limit limit, int value);

		// Sets whether the compiler should automatically assign bindings to uniforms
		// that aren't already explicitly bound in the shader source.
		[DllImport (lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void shaderc_compile_options_set_auto_bind_uniforms (IntPtr options, bool auto_bind);

		// Sets whether the compiler should use HLSL IO mapping rules for bindings.
		// Defaults to false.
		[DllImport (lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void shaderc_compile_options_set_hlsl_io_mapping (IntPtr options, bool hlsl_iomap);

		// Sets whether the compiler should determine block member offsets using HLSL
		// packing rules instead of standard GLSL rules.  Defaults to false.  Only
		// affects GLSL compilation.  HLSL rules are always used when compiling HLSL.
		[DllImport (lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void shaderc_compile_options_set_hlsl_offsets (IntPtr options, bool hlsl_offsets);

		// Sets the base binding number used for for a uniform resource type when
		// automatically assigning bindings.  For GLSL compilation, sets the lowest
		// automatically assigned number.  For HLSL compilation, the regsiter number
		// assigned to the resource is added to this specified base.
		[DllImport (lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void shaderc_compile_options_set_binding_base (IntPtr options, UniformKind kind, UInt32 _base);

		// Like shaderc_compile_options_set_binding_base, but only takes effect when
		// compiling a given shader stage.  The stage is assumed to be one of vertex,
		// fragment, tessellation evaluation, tesselation control, geometry, or compute.
		[DllImport (lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void shaderc_compile_options_set_binding_base_for_stage (IntPtr options, ShaderKind shader_kind, UniformKind kind, UInt32 _base);

		// Sets whether the compiler should automatically assign locations to
		// uniform variables that don't have explicit locations in the shader source.
		[DllImport (lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void shaderc_compile_options_set_auto_map_locations (IntPtr options, bool auto_map);

		// Sets a descriptor set and binding for an HLSL register in the given stage.
		// This method keeps a copy of the string data.
		[DllImport (lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void shaderc_compile_options_set_hlsl_register_set_and_binding_for_stage (IntPtr options, ShaderKind shader_kind, string reg, string set, string binding);

		// Like shaderc_compile_options_set_hlsl_register_set_and_binding_for_stage,
		// but affects all shader stages.
		[DllImport (lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void shaderc_compile_options_set_hlsl_register_set_and_binding (IntPtr options, string reg, string set, string binding);

		// Sets whether the compiler should enable extension
		// SPV_GOOGLE_hlsl_functionality1.
		[DllImport (lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void shaderc_compile_options_set_hlsl_functionality1 (IntPtr options, bool enable);

		// Sets whether the compiler should invert position.Y output in vertex shader.
		[DllImport (lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void shaderc_compile_options_set_invert_y (IntPtr options, bool enable);

		// Sets whether the compiler generates code for max and min builtins which,
		// if given a NaN operand, will return the other operand. Similarly, the clamp
		// builtin will favour the non-NaN operands, as if clamp were implemented
		// as a composition of max and min.
		[DllImport (lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void shaderc_compile_options_set_nan_clamp (IntPtr options, bool enable);



		[DllImport (lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr shaderc_compile_into_spv (
			IntPtr compiler,
			string source,
			UInt64 source_size,
			byte shader_kind,
			string input_file,
			string entry_point,
			IntPtr additional_options);

	}
}
