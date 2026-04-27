// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "VFX/UrpBest1.0"
{
	Properties
	{
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		[ASEBegin][Enum(UnityEngine.Rendering.CullMode)]_CullMode("CullMode", Float) = 2
		[Enum(AddItive,1,AlphaBlend,10)]_dst("Add", Float) = 10
		[HDR]_Tex01Color("Tex01Color", Color) = (1,1,1,1)
		_Tex01("Tex01", 2D) = "white" {}
		_Tex01Rotator("Tex01Rotator", Range( 0 , 1)) = 0
		_TexSpeedU("TexSpeedU", Float) = 0
		_TexSpeedV("TexSpeedV", Float) = 0
		[Toggle(_USERADIAL_ON)] _UseRadial("UseRadial", Float) = 0
		[Toggle(_USETEX01R_ON)] _UseTex01R("UseTex01R(Alpha)", Float) = 0
		[Toggle(_USEPARTICLECUSTOM1_ON)] _UseParticleCustom1("UseParticleCustom1XY", Float) = 0
		[Toggle(_USETEXRAMP_ON)] _UseTexRamp("UseTexRamp", Float) = 0
		_RampTex1("RampTex1", 2D) = "white" {}
		_RampTexSpeedU("RampTexSpeedU", Float) = 0
		_RampTexSpeedV("RampTexSpeedV", Float) = 0
		[Header(Mask)]_Tex01Mask("Tex01Mask", 2D) = "white" {}
		_Tex01Rotator1("Tex01Rotator1", Range( 0 , 1)) = 0
		_Tex1Power("Tex1Power", Float) = 1
		_Mask01SpeedU("Mask01SpeedU", Float) = 0
		_Mask01SpeedV("Mask01SpeedV", Float) = 0
		[Toggle(_USERADIALMASK1_ON)] _UseRadialMask1("UseRadialMask1", Float) = 0
		[Toggle(_USEMASK02_ON)] _UseMask02("UseMask02", Float) = 0
		_Tex02Mask("Tex02Mask", 2D) = "white" {}
		_Tex02Rotator("Tex02Rotator", Range( 0 , 1)) = 0
		_Mask02SpeedU("Mask02SpeedU", Float) = 0
		_Mask02SpeedV("Mask02SpeedV", Float) = 0
		[Toggle(_USERADIALMASK2_ON)] _UseRadialMask2("UseRadialMask2", Float) = 0
		[Header(RaoDong)][Toggle(_USERAODONG_ON)] _UseRaoDong("UseRaoDong", Float) = 0
		_RaoDongTex("RaoDongTex", 2D) = "white" {}
		_RaoDong("RaoDongPower", Range( 0 , 1)) = 0
		_RaoDongTexSpeedU("RaoDongTexSpeedU", Float) = 0
		_RaoDongTexSpeedV("RaoDongTexSpeedV", Float) = 0
		_RaoDongTexMask("RaoDongTexMask", 2D) = "white" {}
		_RaoDongMaskSpeedU1("RaoDongMaskSpeedU", Float) = 0
		_RaoDongMaskSpeedV("RaoDongMaskSpeedV", Float) = 0
		[Toggle(_USEDISSOLVE_ON)] _UseDissolve("UseDissolve", Float) = 0
		_Dissolve("Dissolve", 2D) = "white" {}
		_DissolveRotator2("DissolveRotator2", Range( 0 , 1)) = 0
		_DissolveSpeedU1("DissolveSpeedU", Float) = 0
		_DissolveSpeedV1("DissolveSpeedV", Float) = 0
		[Toggle(_USERADIALDISSOLVE_ON)] _UseRadialDissolve("UseRadialDissolve", Float) = 0
		_DissolveValue2("DissolveValue", Range( 0 , 1)) = 0
		_SoftaDissolve1("SoftaDissolve", Range( 0 , 1)) = 0
		_DissolveWidth1("DissolveWidth", Range( 0 , 1)) = 0
		[HDR]_DissolveColor1("DissolveColor", Color) = (0,0,0,0)
		[Toggle(_USEPARCUSTOM_ON)] _UseParCustom("UseParCustom2(X)", Float) = 0
		[Toggle(_USEDISSOLVEMASK_ON)] _UseDissolveMask("UseDissolveMask", Float) = 0
		_DissolveMask("DissolveMask(OpenCanUse)", 2D) = "white" {}
		_Tex0Rotator3("DissolveMaskRotator", Range( 0 , 1)) = 0
		_DissolveMaskSpeedU2("DissolveMaskSpeedU", Float) = 0
		_DissolveMaskSpeedV("DissolveMaskSpeedV", Float) = 0
		[HideInInspector]_Tex01_ST("Tex1_ST", Vector) = (1,1,0,0)
		[HideInInspector]_Tex01Mask_ST("Tex01Mask_ST", Vector) = (1,1,0,0)
		[HideInInspector]_Tex02Mask_ST("Tex02Mask", Vector) = (1,1,0,0)
		[HideInInspector]_Dissolve_ST("Dissolve", Vector) = (1,1,0,0)
		_Alpha("Alpha", Float) = 1
		[ASEEnd][Enum(Make,1)]_ZhouBoang("ZhouBoang", Float) = 1

		//_TessPhongStrength( "Tess Phong Strength", Range( 0, 1 ) ) = 0.5
		//_TessValue( "Tess Max Tessellation", Range( 1, 32 ) ) = 16
		//_TessMin( "Tess Min Distance", Float ) = 10
		//_TessMax( "Tess Max Distance", Float ) = 25
		//_TessEdgeLength ( "Tess Edge length", Range( 2, 50 ) ) = 16
		//_TessMaxDisp( "Tess Max Displacement", Float ) = 25
	}

	SubShader
	{
		LOD 0

		
		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
		
		Cull [_CullMode]
		AlphaToMask Off
		HLSLINCLUDE
		#pragma target 3.0

		float4 FixedTess( float tessValue )
		{
			return tessValue;
		}
		
		float CalcDistanceTessFactor (float4 vertex, float minDist, float maxDist, float tess, float4x4 o2w, float3 cameraPos )
		{
			float3 wpos = mul(o2w,vertex).xyz;
			float dist = distance (wpos, cameraPos);
			float f = clamp(1.0 - (dist - minDist) / (maxDist - minDist), 0.01, 1.0) * tess;
			return f;
		}

		float4 CalcTriEdgeTessFactors (float3 triVertexFactors)
		{
			float4 tess;
			tess.x = 0.5 * (triVertexFactors.y + triVertexFactors.z);
			tess.y = 0.5 * (triVertexFactors.x + triVertexFactors.z);
			tess.z = 0.5 * (triVertexFactors.x + triVertexFactors.y);
			tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0f;
			return tess;
		}

		float CalcEdgeTessFactor (float3 wpos0, float3 wpos1, float edgeLen, float3 cameraPos, float4 scParams )
		{
			float dist = distance (0.5 * (wpos0+wpos1), cameraPos);
			float len = distance(wpos0, wpos1);
			float f = max(len * scParams.y / (edgeLen * dist), 1.0);
			return f;
		}

		float DistanceFromPlane (float3 pos, float4 plane)
		{
			float d = dot (float4(pos,1.0f), plane);
			return d;
		}

		bool WorldViewFrustumCull (float3 wpos0, float3 wpos1, float3 wpos2, float cullEps, float4 planes[6] )
		{
			float4 planeTest;
			planeTest.x = (( DistanceFromPlane(wpos0, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[0]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.y = (( DistanceFromPlane(wpos0, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[1]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.z = (( DistanceFromPlane(wpos0, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[2]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.w = (( DistanceFromPlane(wpos0, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[3]) > -cullEps) ? 1.0f : 0.0f );
			return !all (planeTest);
		}

		float4 DistanceBasedTess( float4 v0, float4 v1, float4 v2, float tess, float minDist, float maxDist, float4x4 o2w, float3 cameraPos )
		{
			float3 f;
			f.x = CalcDistanceTessFactor (v0,minDist,maxDist,tess,o2w,cameraPos);
			f.y = CalcDistanceTessFactor (v1,minDist,maxDist,tess,o2w,cameraPos);
			f.z = CalcDistanceTessFactor (v2,minDist,maxDist,tess,o2w,cameraPos);

			return CalcTriEdgeTessFactors (f);
		}

		float4 EdgeLengthBasedTess( float4 v0, float4 v1, float4 v2, float edgeLength, float4x4 o2w, float3 cameraPos, float4 scParams )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;
			tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
			tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
			tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
			tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			return tess;
		}

		float4 EdgeLengthBasedTessCull( float4 v0, float4 v1, float4 v2, float edgeLength, float maxDisplacement, float4x4 o2w, float3 cameraPos, float4 scParams, float4 planes[6] )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;

			if (WorldViewFrustumCull(pos0, pos1, pos2, maxDisplacement, planes))
			{
				tess = 0.0f;
			}
			else
			{
				tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
				tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
				tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
				tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			}
			return tess;
		}
		ENDHLSL

		
		Pass
		{
			
			Name "Forward"
			Tags { "LightMode"="UniversalForward" }
			
			Blend SrcAlpha [_dst], One OneMinusSrcAlpha
			ZWrite Off
			ZTest LEqual
			Offset 0 , 0
			ColorMask RGBA
			

			HLSLPROGRAM
			#define _RECEIVE_SHADOWS_OFF 1
			#pragma multi_compile_instancing
			#define ASE_SRP_VERSION 999999

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

			#if ASE_SRP_VERSION <= 70108
			#define REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
			#endif

			#define ASE_NEEDS_FRAG_COLOR
			#pragma shader_feature_local _USEDISSOLVE_ON
			#pragma shader_feature_local _USETEXRAMP_ON
			#pragma shader_feature_local _USERAODONG_ON
			#pragma shader_feature_local _USERADIAL_ON
			#pragma shader_feature_local _USEPARTICLECUSTOM1_ON
			#pragma shader_feature_local _USEDISSOLVEMASK_ON
			#pragma shader_feature_local _USERADIALDISSOLVE_ON
			#pragma shader_feature_local _USEPARCUSTOM_ON
			#pragma shader_feature_local _USETEX01R_ON
			#pragma shader_feature_local _USEMASK02_ON
			#pragma shader_feature_local _USERADIALMASK1_ON
			#pragma shader_feature_local _USERADIALMASK2_ON


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 worldPos : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				float4 shadowCoord : TEXCOORD1;
				#endif
				#ifdef ASE_FOG
				float fogFactor : TEXCOORD2;
				#endif
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_color : COLOR;
				float4 ase_texcoord4 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _Tex01Mask_ST;
			float4 _DissolveMask_ST;
			float4 _Tex02Mask_ST;
			float4 _RampTex1_ST;
			float4 _Tex01_ST;
			float4 _Tex01Color;
			float4 _RaoDongTex_ST;
			float4 _Dissolve_ST;
			float4 _DissolveColor1;
			float4 _RaoDongTexMask_ST;
			float _Tex0Rotator3;
			float _DissolveValue2;
			float _DissolveWidth1;
			float _CullMode;
			float _Mask01SpeedV;
			float _Tex01Rotator1;
			float _Tex1Power;
			float _Mask02SpeedU;
			float _Mask02SpeedV;
			float _Tex02Rotator;
			float _Mask01SpeedU;
			float _DissolveMaskSpeedV;
			float _DissolveSpeedV1;
			float _DissolveRotator2;
			float _dst;
			float _RampTexSpeedU;
			float _RampTexSpeedV;
			float _Tex01Rotator;
			float _RaoDongTexSpeedU;
			float _RaoDongTexSpeedV;
			float _DissolveMaskSpeedU2;
			float _RaoDong;
			float _RaoDongMaskSpeedV;
			float _TexSpeedU;
			float _TexSpeedV;
			float _SoftaDissolve1;
			float _DissolveSpeedU1;
			float _Alpha;
			float _RaoDongMaskSpeedU1;
			float _ZhouBoang;
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END
			sampler2D _RampTex1;
			sampler2D _Tex01;
			sampler2D _RaoDongTex;
			sampler2D _RaoDongTexMask;
			sampler2D _Dissolve;
			sampler2D _DissolveMask;
			sampler2D _Tex01Mask;
			sampler2D _Sampler60196;
			sampler2D _Tex02Mask;
			sampler2D _Sampler60222;


						
			VertexOutput VertexFunction ( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.ase_texcoord3 = v.ase_texcoord;
				o.ase_color = v.ase_color;
				o.ase_texcoord4 = v.ase_texcoord1;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif
				v.ase_normal = v.ase_normal;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				float4 positionCS = TransformWorldToHClip( positionWS );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				o.worldPos = positionWS;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				VertexPositionInputs vertexInput = (VertexPositionInputs)0;
				vertexInput.positionWS = positionWS;
				vertexInput.positionCS = positionCS;
				o.shadowCoord = GetShadowCoord( vertexInput );
				#endif
				#ifdef ASE_FOG
				o.fogFactor = ComputeFogFactor( positionCS.z );
				#endif
				o.clipPos = positionCS;
				return o;
			}

			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.vertex;
				o.ase_normal = v.ase_normal;
				o.ase_texcoord = v.ase_texcoord;
				o.ase_color = v.ase_color;
				o.ase_texcoord1 = v.ase_texcoord1;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
			   return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
				o.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				o.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				o.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
				float phongStrength = _TessPhongStrength;
				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			half4 frag ( VertexOutput IN  ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 WorldPosition = IN.worldPos;
				#endif
				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = IN.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif
				float4 temp_cast_0 = (1.0).xxxx;
				float2 appendResult80 = (float2(_RampTexSpeedU , _RampTexSpeedV));
				float2 uv_RampTex1 = IN.ase_texcoord3.xy * _RampTex1_ST.xy + _RampTex1_ST.zw;
				float2 panner82 = ( 1.0 * _Time.y * appendResult80 + uv_RampTex1);
				#ifdef _USETEXRAMP_ON
				float4 staticSwitch142 = tex2D( _RampTex1, panner82 );
				#else
				float4 staticSwitch142 = temp_cast_0;
				#endif
				float4 RampTex85 = staticSwitch142;
				float2 uv_Tex01 = IN.ase_texcoord3.xy * _Tex01_ST.xy + _Tex01_ST.zw;
				float2 CenteredUV15_g37 = ( IN.ase_texcoord3.xy - float2( 0.5,0.5 ) );
				float2 break17_g37 = CenteredUV15_g37;
				float2 appendResult23_g37 = (float2(( length( CenteredUV15_g37 ) * 1.0 * 2.0 ) , ( atan2( break17_g37.x , break17_g37.y ) * ( 1.0 / TWO_PI ) * 1.0 )));
				float2 appendResult288 = (float2(_Tex01_ST.x , _Tex01_ST.y));
				float2 appendResult289 = (float2(_Tex01_ST.z , _Tex01_ST.w));
				#ifdef _USERADIAL_ON
				float2 staticSwitch168 = (appendResult23_g37*appendResult288 + appendResult289);
				#else
				float2 staticSwitch168 = uv_Tex01;
				#endif
				float cos12 = cos( ( ( _Tex01Rotator * PI ) * 2.0 ) );
				float sin12 = sin( ( ( _Tex01Rotator * PI ) * 2.0 ) );
				float2 rotator12 = mul( staticSwitch168 - float2( 0.5,0.5 ) , float2x2( cos12 , -sin12 , sin12 , cos12 )) + float2( 0.5,0.5 );
				float2 appendResult62 = (float2(_RaoDongTexSpeedU , _RaoDongTexSpeedV));
				float2 uv_RaoDongTex = IN.ase_texcoord3.xy * _RaoDongTex_ST.xy + _RaoDongTex_ST.zw;
				float2 panner58 = ( 1.0 * _Time.y * appendResult62 + uv_RaoDongTex);
				float4 tex2DNode55 = tex2D( _RaoDongTex, panner58 );
				float2 appendResult74 = (float2(_RaoDongMaskSpeedU1 , _RaoDongMaskSpeedV));
				float2 uv_RaoDongTexMask = IN.ase_texcoord3.xy * _RaoDongTexMask_ST.xy + _RaoDongTexMask_ST.zw;
				float2 panner75 = ( 1.0 * _Time.y * appendResult74 + uv_RaoDongTexMask);
				float raodong67 = ( tex2DNode55.r * _RaoDong * tex2D( _RaoDongTexMask, panner75 ).r );
				#ifdef _USERAODONG_ON
				float2 staticSwitch140 = ( rotator12 + raodong67 );
				#else
				float2 staticSwitch140 = rotator12;
				#endif
				float2 appendResult7 = (float2(_TexSpeedU , _TexSpeedV));
				float4 texCoord264 = IN.ase_texcoord3;
				texCoord264.xy = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float2 appendResult265 = (float2(texCoord264.z , texCoord264.w));
				#ifdef _USEPARTICLECUSTOM1_ON
				float2 staticSwitch300 = appendResult265;
				#else
				float2 staticSwitch300 = ( _TimeParameters.x * appendResult7 );
				#endif
				float4 tex2DNode1 = tex2D( _Tex01, ( staticSwitch140 + staticSwitch300 ) );
				float4 temp_output_86_0 = ( RampTex85 * tex2DNode1 * _Tex01Color * float4( (IN.ase_color).rgb , 0.0 ) );
				float temp_output_93_0 = ( 1.0 - _SoftaDissolve1 );
				float4 temp_cast_2 = (temp_output_93_0).xxxx;
				float2 appendResult119 = (float2(_DissolveSpeedU1 , _DissolveSpeedV1));
				float2 uv_Dissolve = IN.ase_texcoord3.xy * _Dissolve_ST.xy + _Dissolve_ST.zw;
				float2 CenteredUV15_g30 = ( IN.ase_texcoord3.xy - float2( 0.5,0.5 ) );
				float2 break17_g30 = CenteredUV15_g30;
				float2 appendResult23_g30 = (float2(( length( CenteredUV15_g30 ) * 1.0 * 2.0 ) , ( atan2( break17_g30.x , break17_g30.y ) * ( 1.0 / TWO_PI ) * 1.0 )));
				float2 appendResult296 = (float2(_Dissolve_ST.x , _Dissolve_ST.y));
				float2 appendResult293 = (float2(_Dissolve_ST.z , _Dissolve_ST.w));
				#ifdef _USERADIALDISSOLVE_ON
				float2 staticSwitch235 = (appendResult23_g30*appendResult296 + appendResult293);
				#else
				float2 staticSwitch235 = uv_Dissolve;
				#endif
				float cos115 = cos( ( ( _DissolveRotator2 * PI ) * 2.0 ) );
				float sin115 = sin( ( ( _DissolveRotator2 * PI ) * 2.0 ) );
				float2 rotator115 = mul( ( staticSwitch235 + float2( 0,0 ) ) - float2( 0.5,0.5 ) , float2x2( cos115 , -sin115 , sin115 , cos115 )) + float2( 0.5,0.5 );
				float2 panner90 = ( 1.0 * _Time.y * appendResult119 + rotator115);
				float4 tex2DNode91 = tex2D( _Dissolve, panner90 );
				float4 temp_cast_3 = (tex2DNode91.r).xxxx;
				float2 appendResult128 = (float2(_DissolveMaskSpeedU2 , _DissolveMaskSpeedV));
				float2 uv_DissolveMask = IN.ase_texcoord3.xy * _DissolveMask_ST.xy + _DissolveMask_ST.zw;
				float cos129 = cos( ( ( _Tex0Rotator3 * PI ) * 2.0 ) );
				float sin129 = sin( ( ( _Tex0Rotator3 * PI ) * 2.0 ) );
				float2 rotator129 = mul( uv_DissolveMask - float2( 0.5,0.5 ) , float2x2( cos129 , -sin129 , sin129 , cos129 )) + float2( 0.5,0.5 );
				float2 panner133 = ( 1.0 * _Time.y * appendResult128 + rotator129);
				#ifdef _USEDISSOLVEMASK_ON
				float4 staticSwitch135 = ( tex2DNode91 * tex2D( _DissolveMask, panner133 ).r );
				#else
				float4 staticSwitch135 = temp_cast_3;
				#endif
				float4 texCoord260 = IN.ase_texcoord4;
				texCoord260.xy = IN.ase_texcoord4.xy * float2( 1,1 ) + float2( 0,0 );
				#ifdef _USEPARCUSTOM_ON
				float staticSwitch259 = texCoord260.z;
				#else
				float staticSwitch259 = _DissolveValue2;
				#endif
				float4 temp_cast_4 = (( staticSwitch259 * 2.0 )).xxxx;
				float4 temp_output_97_0 = ( ( staticSwitch135 + 1.0 ) - temp_cast_4 );
				float4 smoothstepResult99 = smoothstep( float4( 0,0,0,0 ) , temp_cast_2 , temp_output_97_0);
				float4 temp_cast_5 = (( temp_output_93_0 + _DissolveWidth1 )).xxxx;
				float4 temp_cast_6 = (( staticSwitch259 * 2.0 )).xxxx;
				float4 smoothstepResult98 = smoothstep( float4( 0,0,0,0 ) , temp_cast_5 , temp_output_97_0);
				float4 DissolveWidth104 = ( ( smoothstepResult99 - smoothstepResult98 ) * _DissolveColor1 );
				#ifdef _USEDISSOLVE_ON
				float4 staticSwitch146 = ( DissolveWidth104 + temp_output_86_0 );
				#else
				float4 staticSwitch146 = temp_output_86_0;
				#endif
				
				#ifdef _USETEX01R_ON
				float staticSwitch48 = tex2DNode1.r;
				#else
				float staticSwitch48 = tex2DNode1.a;
				#endif
				float2 appendResult24 = (float2(_Mask01SpeedU , _Mask01SpeedV));
				float2 uv_Tex01Mask = IN.ase_texcoord3.xy * _Tex01Mask_ST.xy + _Tex01Mask_ST.zw;
				float2 temp_output_1_0_g33 = float2( 1,1 );
				float2 texCoord80_g33 = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float2 appendResult10_g33 = (float2(( (temp_output_1_0_g33).x * texCoord80_g33.x ) , ( texCoord80_g33.y * (temp_output_1_0_g33).y )));
				float2 temp_output_11_0_g33 = float2( 0,0 );
				float2 texCoord81_g33 = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float2 panner18_g33 = ( ( (temp_output_11_0_g33).x * _TimeParameters.x ) * float2( 1,0 ) + texCoord81_g33);
				float2 panner19_g33 = ( ( _TimeParameters.x * (temp_output_11_0_g33).y ) * float2( 0,1 ) + texCoord81_g33);
				float2 appendResult24_g33 = (float2((panner18_g33).x , (panner19_g33).y));
				float2 Mask1Speed206 = appendResult24;
				float2 temp_output_47_0_g33 = Mask1Speed206;
				float2 texCoord78_g33 = IN.ase_texcoord3.xy * float2( 2,2 ) + float2( 0,0 );
				float2 temp_output_31_0_g33 = ( texCoord78_g33 - float2( 1,1 ) );
				float2 appendResult39_g33 = (float2(frac( ( atan2( (temp_output_31_0_g33).x , (temp_output_31_0_g33).y ) / TWO_PI ) ) , length( temp_output_31_0_g33 )));
				float2 panner54_g33 = ( ( (temp_output_47_0_g33).x * _TimeParameters.x ) * float2( 1,0 ) + appendResult39_g33);
				float2 panner55_g33 = ( ( _TimeParameters.x * (temp_output_47_0_g33).y ) * float2( 0,1 ) + appendResult39_g33);
				float2 appendResult58_g33 = (float2((panner54_g33).x , (panner55_g33).y));
				#ifdef _USERADIALMASK1_ON
				float2 staticSwitch198 = ( ( (tex2D( _Sampler60196, ( appendResult10_g33 + appendResult24_g33 ) )).rg * 1.0 ) + ( _Tex01Mask_ST.xy * appendResult58_g33 ) );
				#else
				float2 staticSwitch198 = uv_Tex01Mask;
				#endif
				float2 appendResult197 = (float2(_Tex01Mask_ST.z , _Tex01Mask_ST.w));
				float cos20 = cos( ( ( _Tex01Rotator1 * PI ) * 2.0 ) );
				float sin20 = sin( ( ( _Tex01Rotator1 * PI ) * 2.0 ) );
				float2 rotator20 = mul( ( staticSwitch198 + appendResult197 ) - float2( 0.5,0.5 ) , float2x2( cos20 , -sin20 , sin20 , cos20 )) + float2( 0.5,0.5 );
				float2 panner18 = ( 1.0 * _Time.y * appendResult24 + rotator20);
				float temp_output_186_0 = pow( tex2D( _Tex01Mask, panner18 ).r , _Tex1Power );
				float2 appendResult39 = (float2(_Mask02SpeedU , _Mask02SpeedV));
				float2 uv_Tex02Mask = IN.ase_texcoord3.xy * _Tex02Mask_ST.xy + _Tex02Mask_ST.zw;
				float2 temp_output_1_0_g36 = float2( 1,1 );
				float2 texCoord80_g36 = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float2 appendResult10_g36 = (float2(( (temp_output_1_0_g36).x * texCoord80_g36.x ) , ( texCoord80_g36.y * (temp_output_1_0_g36).y )));
				float2 temp_output_11_0_g36 = float2( 0,0 );
				float2 texCoord81_g36 = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float2 panner18_g36 = ( ( (temp_output_11_0_g36).x * _TimeParameters.x ) * float2( 1,0 ) + texCoord81_g36);
				float2 panner19_g36 = ( ( _TimeParameters.x * (temp_output_11_0_g36).y ) * float2( 0,1 ) + texCoord81_g36);
				float2 appendResult24_g36 = (float2((panner18_g36).x , (panner19_g36).y));
				float2 Mask2Speed228 = appendResult39;
				float2 temp_output_47_0_g36 = Mask2Speed228;
				float2 texCoord78_g36 = IN.ase_texcoord3.xy * float2( 2,2 ) + float2( 0,0 );
				float2 temp_output_31_0_g36 = ( texCoord78_g36 - float2( 1,1 ) );
				float2 appendResult39_g36 = (float2(frac( ( atan2( (temp_output_31_0_g36).x , (temp_output_31_0_g36).y ) / TWO_PI ) ) , length( temp_output_31_0_g36 )));
				float2 panner54_g36 = ( ( (temp_output_47_0_g36).x * _TimeParameters.x ) * float2( 1,0 ) + appendResult39_g36);
				float2 panner55_g36 = ( ( _TimeParameters.x * (temp_output_47_0_g36).y ) * float2( 0,1 ) + appendResult39_g36);
				float2 appendResult58_g36 = (float2((panner54_g36).x , (panner55_g36).y));
				#ifdef _USERADIALMASK2_ON
				float2 staticSwitch223 = ( ( (tex2D( _Sampler60222, ( appendResult10_g36 + appendResult24_g36 ) )).rg * 1.0 ) + ( _Tex02Mask_ST.xy * appendResult58_g36 ) );
				#else
				float2 staticSwitch223 = uv_Tex02Mask;
				#endif
				float2 appendResult220 = (float2(_Tex02Mask_ST.z , _Tex02Mask_ST.w));
				float cos40 = cos( ( ( _Tex02Rotator * PI ) * 2.0 ) );
				float sin40 = sin( ( ( _Tex02Rotator * PI ) * 2.0 ) );
				float2 rotator40 = mul( ( staticSwitch223 + appendResult220 ) - float2( 0.5,0.5 ) , float2x2( cos40 , -sin40 , sin40 , cos40 )) + float2( 0.5,0.5 );
				float2 panner41 = ( 1.0 * _Time.y * appendResult39 + rotator40);
				float4 tex2DNode42 = tex2D( _Tex02Mask, panner41 );
				#ifdef _USEMASK02_ON
				float staticSwitch44 = ( temp_output_186_0 * tex2DNode42.r );
				#else
				float staticSwitch44 = temp_output_186_0;
				#endif
				float Mask0129 = staticSwitch44;
				float4 temp_cast_11 = (1.0).xxxx;
				float4 DissolveMask103 = smoothstepResult99;
				#ifdef _USEDISSOLVE_ON
				float4 staticSwitch147 = DissolveMask103;
				#else
				float4 staticSwitch147 = temp_cast_11;
				#endif
				
				float3 BakedAlbedo = 0;
				float3 BakedEmission = 0;
				float3 Color = staticSwitch146.rgb;
				float Alpha = ( staticSwitch48 * Mask0129 * staticSwitch147 * IN.ase_color.a * _Alpha * _Tex01Color.a * _ZhouBoang ).r;
				float AlphaClipThreshold = 0.5;
				float AlphaClipThresholdShadow = 0.5;

				#ifdef _ALPHATEST_ON
					clip( Alpha - AlphaClipThreshold );
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif

				#ifdef ASE_FOG
					Color = MixFog( Color, IN.fogFactor );
				#endif

				return half4( Color, Alpha );
			}

			ENDHLSL
		}

		
		Pass
		{
			
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }

			ZWrite On
			ColorMask 0
			AlphaToMask Off

			HLSLPROGRAM
			#define _RECEIVE_SHADOWS_OFF 1
			#pragma multi_compile_instancing
			#define ASE_SRP_VERSION 999999

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			#pragma shader_feature_local _USETEX01R_ON
			#pragma shader_feature_local _USERAODONG_ON
			#pragma shader_feature_local _USERADIAL_ON
			#pragma shader_feature_local _USEPARTICLECUSTOM1_ON
			#pragma shader_feature_local _USEMASK02_ON
			#pragma shader_feature_local _USERADIALMASK1_ON
			#pragma shader_feature_local _USERADIALMASK2_ON
			#pragma shader_feature_local _USEDISSOLVE_ON
			#pragma shader_feature_local _USEDISSOLVEMASK_ON
			#pragma shader_feature_local _USERADIALDISSOLVE_ON
			#pragma shader_feature_local _USEPARCUSTOM_ON


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 worldPos : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				float4 shadowCoord : TEXCOORD1;
				#endif
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _Tex01Mask_ST;
			float4 _DissolveMask_ST;
			float4 _Tex02Mask_ST;
			float4 _RampTex1_ST;
			float4 _Tex01_ST;
			float4 _Tex01Color;
			float4 _RaoDongTex_ST;
			float4 _Dissolve_ST;
			float4 _DissolveColor1;
			float4 _RaoDongTexMask_ST;
			float _Tex0Rotator3;
			float _DissolveValue2;
			float _DissolveWidth1;
			float _CullMode;
			float _Mask01SpeedV;
			float _Tex01Rotator1;
			float _Tex1Power;
			float _Mask02SpeedU;
			float _Mask02SpeedV;
			float _Tex02Rotator;
			float _Mask01SpeedU;
			float _DissolveMaskSpeedV;
			float _DissolveSpeedV1;
			float _DissolveRotator2;
			float _dst;
			float _RampTexSpeedU;
			float _RampTexSpeedV;
			float _Tex01Rotator;
			float _RaoDongTexSpeedU;
			float _RaoDongTexSpeedV;
			float _DissolveMaskSpeedU2;
			float _RaoDong;
			float _RaoDongMaskSpeedV;
			float _TexSpeedU;
			float _TexSpeedV;
			float _SoftaDissolve1;
			float _DissolveSpeedU1;
			float _Alpha;
			float _RaoDongMaskSpeedU1;
			float _ZhouBoang;
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END
			sampler2D _Tex01;
			sampler2D _RaoDongTex;
			sampler2D _RaoDongTexMask;
			sampler2D _Tex01Mask;
			sampler2D _Sampler60196;
			sampler2D _Tex02Mask;
			sampler2D _Sampler60222;
			sampler2D _Dissolve;
			sampler2D _DissolveMask;


			
			VertexOutput VertexFunction( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.ase_texcoord2 = v.ase_texcoord;
				o.ase_texcoord3 = v.ase_texcoord1;
				o.ase_color = v.ase_color;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = v.ase_normal;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				o.worldPos = positionWS;
				#endif

				o.clipPos = TransformWorldToHClip( positionWS );
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
					vertexInput.positionWS = positionWS;
					vertexInput.positionCS = clipPos;
					o.shadowCoord = GetShadowCoord( vertexInput );
				#endif
				return o;
			}

			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.vertex;
				o.ase_normal = v.ase_normal;
				o.ase_texcoord = v.ase_texcoord;
				o.ase_texcoord1 = v.ase_texcoord1;
				o.ase_color = v.ase_color;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
			   return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
				o.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				o.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
				o.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
				float phongStrength = _TessPhongStrength;
				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			half4 frag(VertexOutput IN  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 WorldPosition = IN.worldPos;
				#endif
				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = IN.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				float2 uv_Tex01 = IN.ase_texcoord2.xy * _Tex01_ST.xy + _Tex01_ST.zw;
				float2 CenteredUV15_g37 = ( IN.ase_texcoord2.xy - float2( 0.5,0.5 ) );
				float2 break17_g37 = CenteredUV15_g37;
				float2 appendResult23_g37 = (float2(( length( CenteredUV15_g37 ) * 1.0 * 2.0 ) , ( atan2( break17_g37.x , break17_g37.y ) * ( 1.0 / TWO_PI ) * 1.0 )));
				float2 appendResult288 = (float2(_Tex01_ST.x , _Tex01_ST.y));
				float2 appendResult289 = (float2(_Tex01_ST.z , _Tex01_ST.w));
				#ifdef _USERADIAL_ON
				float2 staticSwitch168 = (appendResult23_g37*appendResult288 + appendResult289);
				#else
				float2 staticSwitch168 = uv_Tex01;
				#endif
				float cos12 = cos( ( ( _Tex01Rotator * PI ) * 2.0 ) );
				float sin12 = sin( ( ( _Tex01Rotator * PI ) * 2.0 ) );
				float2 rotator12 = mul( staticSwitch168 - float2( 0.5,0.5 ) , float2x2( cos12 , -sin12 , sin12 , cos12 )) + float2( 0.5,0.5 );
				float2 appendResult62 = (float2(_RaoDongTexSpeedU , _RaoDongTexSpeedV));
				float2 uv_RaoDongTex = IN.ase_texcoord2.xy * _RaoDongTex_ST.xy + _RaoDongTex_ST.zw;
				float2 panner58 = ( 1.0 * _Time.y * appendResult62 + uv_RaoDongTex);
				float4 tex2DNode55 = tex2D( _RaoDongTex, panner58 );
				float2 appendResult74 = (float2(_RaoDongMaskSpeedU1 , _RaoDongMaskSpeedV));
				float2 uv_RaoDongTexMask = IN.ase_texcoord2.xy * _RaoDongTexMask_ST.xy + _RaoDongTexMask_ST.zw;
				float2 panner75 = ( 1.0 * _Time.y * appendResult74 + uv_RaoDongTexMask);
				float raodong67 = ( tex2DNode55.r * _RaoDong * tex2D( _RaoDongTexMask, panner75 ).r );
				#ifdef _USERAODONG_ON
				float2 staticSwitch140 = ( rotator12 + raodong67 );
				#else
				float2 staticSwitch140 = rotator12;
				#endif
				float2 appendResult7 = (float2(_TexSpeedU , _TexSpeedV));
				float4 texCoord264 = IN.ase_texcoord2;
				texCoord264.xy = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 appendResult265 = (float2(texCoord264.z , texCoord264.w));
				#ifdef _USEPARTICLECUSTOM1_ON
				float2 staticSwitch300 = appendResult265;
				#else
				float2 staticSwitch300 = ( _TimeParameters.x * appendResult7 );
				#endif
				float4 tex2DNode1 = tex2D( _Tex01, ( staticSwitch140 + staticSwitch300 ) );
				#ifdef _USETEX01R_ON
				float staticSwitch48 = tex2DNode1.r;
				#else
				float staticSwitch48 = tex2DNode1.a;
				#endif
				float2 appendResult24 = (float2(_Mask01SpeedU , _Mask01SpeedV));
				float2 uv_Tex01Mask = IN.ase_texcoord2.xy * _Tex01Mask_ST.xy + _Tex01Mask_ST.zw;
				float2 temp_output_1_0_g33 = float2( 1,1 );
				float2 texCoord80_g33 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 appendResult10_g33 = (float2(( (temp_output_1_0_g33).x * texCoord80_g33.x ) , ( texCoord80_g33.y * (temp_output_1_0_g33).y )));
				float2 temp_output_11_0_g33 = float2( 0,0 );
				float2 texCoord81_g33 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 panner18_g33 = ( ( (temp_output_11_0_g33).x * _TimeParameters.x ) * float2( 1,0 ) + texCoord81_g33);
				float2 panner19_g33 = ( ( _TimeParameters.x * (temp_output_11_0_g33).y ) * float2( 0,1 ) + texCoord81_g33);
				float2 appendResult24_g33 = (float2((panner18_g33).x , (panner19_g33).y));
				float2 Mask1Speed206 = appendResult24;
				float2 temp_output_47_0_g33 = Mask1Speed206;
				float2 texCoord78_g33 = IN.ase_texcoord2.xy * float2( 2,2 ) + float2( 0,0 );
				float2 temp_output_31_0_g33 = ( texCoord78_g33 - float2( 1,1 ) );
				float2 appendResult39_g33 = (float2(frac( ( atan2( (temp_output_31_0_g33).x , (temp_output_31_0_g33).y ) / TWO_PI ) ) , length( temp_output_31_0_g33 )));
				float2 panner54_g33 = ( ( (temp_output_47_0_g33).x * _TimeParameters.x ) * float2( 1,0 ) + appendResult39_g33);
				float2 panner55_g33 = ( ( _TimeParameters.x * (temp_output_47_0_g33).y ) * float2( 0,1 ) + appendResult39_g33);
				float2 appendResult58_g33 = (float2((panner54_g33).x , (panner55_g33).y));
				#ifdef _USERADIALMASK1_ON
				float2 staticSwitch198 = ( ( (tex2D( _Sampler60196, ( appendResult10_g33 + appendResult24_g33 ) )).rg * 1.0 ) + ( _Tex01Mask_ST.xy * appendResult58_g33 ) );
				#else
				float2 staticSwitch198 = uv_Tex01Mask;
				#endif
				float2 appendResult197 = (float2(_Tex01Mask_ST.z , _Tex01Mask_ST.w));
				float cos20 = cos( ( ( _Tex01Rotator1 * PI ) * 2.0 ) );
				float sin20 = sin( ( ( _Tex01Rotator1 * PI ) * 2.0 ) );
				float2 rotator20 = mul( ( staticSwitch198 + appendResult197 ) - float2( 0.5,0.5 ) , float2x2( cos20 , -sin20 , sin20 , cos20 )) + float2( 0.5,0.5 );
				float2 panner18 = ( 1.0 * _Time.y * appendResult24 + rotator20);
				float temp_output_186_0 = pow( tex2D( _Tex01Mask, panner18 ).r , _Tex1Power );
				float2 appendResult39 = (float2(_Mask02SpeedU , _Mask02SpeedV));
				float2 uv_Tex02Mask = IN.ase_texcoord2.xy * _Tex02Mask_ST.xy + _Tex02Mask_ST.zw;
				float2 temp_output_1_0_g36 = float2( 1,1 );
				float2 texCoord80_g36 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 appendResult10_g36 = (float2(( (temp_output_1_0_g36).x * texCoord80_g36.x ) , ( texCoord80_g36.y * (temp_output_1_0_g36).y )));
				float2 temp_output_11_0_g36 = float2( 0,0 );
				float2 texCoord81_g36 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 panner18_g36 = ( ( (temp_output_11_0_g36).x * _TimeParameters.x ) * float2( 1,0 ) + texCoord81_g36);
				float2 panner19_g36 = ( ( _TimeParameters.x * (temp_output_11_0_g36).y ) * float2( 0,1 ) + texCoord81_g36);
				float2 appendResult24_g36 = (float2((panner18_g36).x , (panner19_g36).y));
				float2 Mask2Speed228 = appendResult39;
				float2 temp_output_47_0_g36 = Mask2Speed228;
				float2 texCoord78_g36 = IN.ase_texcoord2.xy * float2( 2,2 ) + float2( 0,0 );
				float2 temp_output_31_0_g36 = ( texCoord78_g36 - float2( 1,1 ) );
				float2 appendResult39_g36 = (float2(frac( ( atan2( (temp_output_31_0_g36).x , (temp_output_31_0_g36).y ) / TWO_PI ) ) , length( temp_output_31_0_g36 )));
				float2 panner54_g36 = ( ( (temp_output_47_0_g36).x * _TimeParameters.x ) * float2( 1,0 ) + appendResult39_g36);
				float2 panner55_g36 = ( ( _TimeParameters.x * (temp_output_47_0_g36).y ) * float2( 0,1 ) + appendResult39_g36);
				float2 appendResult58_g36 = (float2((panner54_g36).x , (panner55_g36).y));
				#ifdef _USERADIALMASK2_ON
				float2 staticSwitch223 = ( ( (tex2D( _Sampler60222, ( appendResult10_g36 + appendResult24_g36 ) )).rg * 1.0 ) + ( _Tex02Mask_ST.xy * appendResult58_g36 ) );
				#else
				float2 staticSwitch223 = uv_Tex02Mask;
				#endif
				float2 appendResult220 = (float2(_Tex02Mask_ST.z , _Tex02Mask_ST.w));
				float cos40 = cos( ( ( _Tex02Rotator * PI ) * 2.0 ) );
				float sin40 = sin( ( ( _Tex02Rotator * PI ) * 2.0 ) );
				float2 rotator40 = mul( ( staticSwitch223 + appendResult220 ) - float2( 0.5,0.5 ) , float2x2( cos40 , -sin40 , sin40 , cos40 )) + float2( 0.5,0.5 );
				float2 panner41 = ( 1.0 * _Time.y * appendResult39 + rotator40);
				float4 tex2DNode42 = tex2D( _Tex02Mask, panner41 );
				#ifdef _USEMASK02_ON
				float staticSwitch44 = ( temp_output_186_0 * tex2DNode42.r );
				#else
				float staticSwitch44 = temp_output_186_0;
				#endif
				float Mask0129 = staticSwitch44;
				float4 temp_cast_2 = (1.0).xxxx;
				float temp_output_93_0 = ( 1.0 - _SoftaDissolve1 );
				float4 temp_cast_3 = (temp_output_93_0).xxxx;
				float2 appendResult119 = (float2(_DissolveSpeedU1 , _DissolveSpeedV1));
				float2 uv_Dissolve = IN.ase_texcoord2.xy * _Dissolve_ST.xy + _Dissolve_ST.zw;
				float2 CenteredUV15_g30 = ( IN.ase_texcoord2.xy - float2( 0.5,0.5 ) );
				float2 break17_g30 = CenteredUV15_g30;
				float2 appendResult23_g30 = (float2(( length( CenteredUV15_g30 ) * 1.0 * 2.0 ) , ( atan2( break17_g30.x , break17_g30.y ) * ( 1.0 / TWO_PI ) * 1.0 )));
				float2 appendResult296 = (float2(_Dissolve_ST.x , _Dissolve_ST.y));
				float2 appendResult293 = (float2(_Dissolve_ST.z , _Dissolve_ST.w));
				#ifdef _USERADIALDISSOLVE_ON
				float2 staticSwitch235 = (appendResult23_g30*appendResult296 + appendResult293);
				#else
				float2 staticSwitch235 = uv_Dissolve;
				#endif
				float cos115 = cos( ( ( _DissolveRotator2 * PI ) * 2.0 ) );
				float sin115 = sin( ( ( _DissolveRotator2 * PI ) * 2.0 ) );
				float2 rotator115 = mul( ( staticSwitch235 + float2( 0,0 ) ) - float2( 0.5,0.5 ) , float2x2( cos115 , -sin115 , sin115 , cos115 )) + float2( 0.5,0.5 );
				float2 panner90 = ( 1.0 * _Time.y * appendResult119 + rotator115);
				float4 tex2DNode91 = tex2D( _Dissolve, panner90 );
				float4 temp_cast_4 = (tex2DNode91.r).xxxx;
				float2 appendResult128 = (float2(_DissolveMaskSpeedU2 , _DissolveMaskSpeedV));
				float2 uv_DissolveMask = IN.ase_texcoord2.xy * _DissolveMask_ST.xy + _DissolveMask_ST.zw;
				float cos129 = cos( ( ( _Tex0Rotator3 * PI ) * 2.0 ) );
				float sin129 = sin( ( ( _Tex0Rotator3 * PI ) * 2.0 ) );
				float2 rotator129 = mul( uv_DissolveMask - float2( 0.5,0.5 ) , float2x2( cos129 , -sin129 , sin129 , cos129 )) + float2( 0.5,0.5 );
				float2 panner133 = ( 1.0 * _Time.y * appendResult128 + rotator129);
				#ifdef _USEDISSOLVEMASK_ON
				float4 staticSwitch135 = ( tex2DNode91 * tex2D( _DissolveMask, panner133 ).r );
				#else
				float4 staticSwitch135 = temp_cast_4;
				#endif
				float4 texCoord260 = IN.ase_texcoord3;
				texCoord260.xy = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				#ifdef _USEPARCUSTOM_ON
				float staticSwitch259 = texCoord260.z;
				#else
				float staticSwitch259 = _DissolveValue2;
				#endif
				float4 temp_cast_5 = (( staticSwitch259 * 2.0 )).xxxx;
				float4 temp_output_97_0 = ( ( staticSwitch135 + 1.0 ) - temp_cast_5 );
				float4 smoothstepResult99 = smoothstep( float4( 0,0,0,0 ) , temp_cast_3 , temp_output_97_0);
				float4 DissolveMask103 = smoothstepResult99;
				#ifdef _USEDISSOLVE_ON
				float4 staticSwitch147 = DissolveMask103;
				#else
				float4 staticSwitch147 = temp_cast_2;
				#endif
				
				float Alpha = ( staticSwitch48 * Mask0129 * staticSwitch147 * IN.ase_color.a * _Alpha * _Tex01Color.a * _ZhouBoang ).r;
				float AlphaClipThreshold = 0.5;

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif
				return 0;
			}
			ENDHLSL
		}

	
	}
	CustomEditor "UnityEditor.ShaderGraph.PBRMasterGUI"
	Fallback "Hidden/InternalErrorShader"
	
}
/*ASEBEGIN
Version=18800
546.6667;72.66667;1659.333;1013.667;10000.49;3891.757;10.37973;True;False
Node;AmplifyShaderEditor.CommentaryNode;88;-8239.489,-2279.676;Inherit;False;5509.562;1998.947;Comment;55;260;149;236;245;104;103;102;101;100;99;98;97;96;95;94;105;93;106;259;92;124;125;91;133;90;128;129;115;126;116;134;127;131;130;122;117;118;123;132;113;235;114;89;232;119;120;121;290;292;293;294;295;296;297;135;溶解部分;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;27;-5592.726,746.8015;Inherit;False;3009.502;2259.677;;40;226;206;228;229;194;221;222;197;220;198;227;223;199;196;43;29;44;45;42;186;187;41;17;39;40;18;38;36;37;35;24;20;25;26;21;34;19;33;22;23;Mask01;1,1,1,1;0;0
Node;AmplifyShaderEditor.Vector4Node;232;-8188.545,-1680.912;Inherit;False;Property;_Dissolve_ST;Dissolve;54;1;[HideInInspector];Create;False;0;0;0;False;0;False;1,1,0,0;1,1,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;25;-4177.892,1158.825;Inherit;False;Property;_Mask01SpeedV;Mask01SpeedV;18;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;26;-4178.481,1067.447;Inherit;False;Property;_Mask01SpeedU;Mask01SpeedU;17;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;132;-6901.64,-961.094;Inherit;False;Property;_Tex0Rotator3;DissolveMaskRotator;48;0;Create;False;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;293;-7918.022,-1629.472;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;294;-8030.245,-1951.503;Inherit;True;Polar Coordinates;-1;;30;7dab8e02884cf104ebefaa2e788e4162;0;4;1;FLOAT2;0,0;False;2;FLOAT2;0.5,0.5;False;3;FLOAT;1;False;4;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;296;-7916.805,-1722.202;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;89;-8089.555,-2179.317;Inherit;False;0;91;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ScaleAndOffsetNode;295;-7627.596,-1968.074;Inherit;True;3;0;FLOAT2;0,0;False;1;FLOAT2;1,0;False;2;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;37;-4147.495,2332.355;Inherit;False;Property;_Mask02SpeedV;Mask02SpeedV;24;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;118;-6763.024,-1894.841;Inherit;False;Property;_DissolveRotator2;DissolveRotator2;36;0;Create;False;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.PiNode;130;-6795.774,-1079.961;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;35;-4131.569,2229.828;Inherit;False;Property;_Mask02SpeedU;Mask02SpeedU;23;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;24;-3970.476,1099.447;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PiNode;117;-6676.419,-1969.778;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;127;-6625.303,-1057.694;Inherit;False;Property;_DissolveMaskSpeedU2;DissolveMaskSpeedU;49;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;39;-3933.219,2274.109;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;206;-3785.377,1200.74;Inherit;False;Mask1Speed;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;134;-7127.083,-1249.606;Inherit;False;0;125;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;126;-6616.302,-959.6119;Inherit;False;Property;_DissolveMaskSpeedV;DissolveMaskSpeedV;50;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;131;-6759.217,-1185.061;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;235;-7575.513,-2178.763;Inherit;False;Property;_UseRadialDissolve;UseRadialDissolve;39;0;Create;False;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;63;-5402.5,-283.5226;Inherit;False;1539.718;987.8471;;16;67;56;77;57;75;76;74;73;72;110;55;58;59;62;61;60;扰动效果;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;121;-6457.105,-1882.893;Inherit;False;Property;_DissolveSpeedV1;DissolveSpeedV;38;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;228;-3716.144,2287.409;Inherit;False;Mask2Speed;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;226;-5494.248,1451.05;Inherit;False;206;Mask1Speed;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;72;-5266.088,546.9592;Inherit;False;Property;_RaoDongMaskSpeedV;RaoDongMaskSpeedV;33;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;236;-7132.101,-2160.827;Inherit;True;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RotatorNode;129;-6585.568,-1245.58;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector4Node;194;-5503.218,1198.115;Inherit;False;Property;_Tex01Mask_ST;Tex01Mask_ST;52;1;[HideInInspector];Create;False;0;0;0;False;0;False;1,1,0,0;-1,1,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;120;-6457.993,-1974.271;Inherit;False;Property;_DissolveSpeedU1;DissolveSpeedU;37;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;61;-5335.701,140.4493;Inherit;False;Property;_RaoDongTexSpeedV;RaoDongTexSpeedV;30;0;Create;False;0;0;0;False;0;False;0;-0.3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;73;-5256.55,461.6601;Inherit;False;Property;_RaoDongMaskSpeedU1;RaoDongMaskSpeedU;32;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;128;-6401.339,-1014.36;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;116;-6641.439,-2055.956;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;60;-5337.302,44.64999;Inherit;False;Property;_RaoDongTexSpeedU;RaoDongTexSpeedU;29;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;133;-6300.34,-1252.666;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;59;-5334.998,-114.8787;Inherit;False;0;55;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;119;-6253.079,-1943.571;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;23;-4545.132,1213.536;Inherit;False;Property;_Tex01Rotator1;Tex01Rotator1;15;0;Create;False;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;74;-5039.812,506.2206;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RotatorNode;115;-6506.156,-2166.277;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;19;-5530.347,842.3751;Inherit;False;0;17;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;62;-5136.721,87.59087;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;76;-5236.088,305.7511;Inherit;False;0;77;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;229;-5520,2528;Inherit;False;228;Mask2Speed;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector4Node;221;-5568,2256;Inherit;False;Property;_Tex02Mask_ST;Tex02Mask;53;1;[HideInInspector];Create;False;0;0;0;False;0;False;1,1,0,0;1,1,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;196;-5300.054,1122.177;Inherit;False;RadialUVDistortion;-1;;33;051d65e7699b41a4c800363fd0e822b2;0;7;60;SAMPLER2D;_Sampler60196;False;1;FLOAT2;1,1;False;11;FLOAT2;0,0;False;65;FLOAT;1;False;68;FLOAT2;1,1;False;47;FLOAT2;1,1;False;29;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;38;-5302.307,2024.918;Inherit;False;0;42;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StaticSwitch;198;-5021.94,858.0745;Inherit;False;Property;_UseRadialMask1;UseRadialMask1;19;0;Create;False;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;33;-4540.309,2363.818;Inherit;False;Property;_Tex02Rotator;Tex02Rotator;22;0;Create;False;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;90;-6298.794,-2168.513;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;197;-5028.054,1362.177;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;75;-4910.82,308.1812;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector4Node;164;-2802.726,319.6408;Inherit;False;Property;_Tex01_ST;Tex1_ST;51;1;[HideInInspector];Create;False;0;0;0;False;0;False;1,1,0,0;1,2,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;125;-6089.454,-1263.533;Inherit;True;Property;_DissolveMask;DissolveMask(OpenCanUse);47;0;Create;False;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;58;-5007.729,-110.4487;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PiNode;22;-4466.512,1116.208;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;160;-1803.354,-199.038;Inherit;False;349.2452;471.1461;Comment;4;12;15;14;13;旋转;1,1,1,1;0;0
Node;AmplifyShaderEditor.FunctionNode;222;-5328,2192;Inherit;False;RadialUVDistortion;-1;;36;051d65e7699b41a4c800363fd0e822b2;0;7;60;SAMPLER2D;_Sampler60222;False;1;FLOAT2;1,1;False;11;FLOAT2;0,0;False;65;FLOAT;1;False;68;FLOAT2;1,1;False;47;FLOAT2;1,1;False;29;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;289;-2578.726,415.6407;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;286;-2738.726,47.64069;Inherit;True;Polar Coordinates;-1;;37;7dab8e02884cf104ebefaa2e788e4162;0;4;1;FLOAT2;0,0;False;2;FLOAT2;0.5,0.5;False;3;FLOAT;1;False;4;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;55;-4765.457,-142.5936;Inherit;True;Property;_RaoDongTex;RaoDongTex;27;0;Create;False;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;288;-2578.726,319.6408;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;57;-4692.113,68.08389;Inherit;False;Property;_RaoDong;RaoDongPower;28;0;Create;False;0;0;0;False;0;False;0;0.01;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;91;-6048.957,-2194.754;Inherit;True;Property;_Dissolve;Dissolve;35;0;Create;False;0;0;0;False;0;False;-1;None;5aef9b5b75f57704395b29499bb8f904;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;77;-4680.901,282.2124;Inherit;True;Property;_RaoDongTexMask;RaoDongTexMask;31;0;Create;False;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;13;-1753.354,156.9948;Inherit;False;Property;_Tex01Rotator;Tex01Rotator;4;0;Create;False;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.PiNode;34;-4461.333,2269.246;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;297;-5819.619,-1811.891;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;220;-5056,2432;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;21;-4428.816,1016.318;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;223;-4978.407,2024.799;Inherit;False;Property;_UseRadialMask2;UseRadialMask2;25;0;Create;False;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;199;-4709.955,860.3582;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;227;-4697.554,2028.307;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;56;-4317.702,-109.3885;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PiNode;14;-1729.639,87.24408;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;3;-2760.204,-146.9156;Inherit;False;0;1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;124;-5682.965,-2037.899;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;287;-2338.726,31.6407;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;1,0;False;2;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;92;-5552.534,-1767.768;Inherit;True;Property;_DissolveValue2;DissolveValue;40;0;Create;False;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;36;-4453.205,2171.334;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;260;-5590.591,-1540.219;Inherit;True;1;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RotatorNode;20;-4300.816,856.318;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;11;-1416.483,401.3399;Inherit;False;Property;_TexSpeedV;TexSpeedV;6;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;292;-5151.22,-2025.33;Inherit;True;Constant;_Float5;Float 5;58;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;5;-1418.084,324.7408;Inherit;False;Property;_TexSpeedU;TexSpeedU;5;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;259;-5166.322,-1789.798;Inherit;True;Property;_UseParCustom;UseParCustom2(X);44;0;Create;False;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode;40;-4293.988,2031.662;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StaticSwitch;168;-2114.499,-139.3391;Inherit;True;Property;_UseRadial;UseRadial;7;0;Create;False;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;15;-1672.542,-22.0305;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;67;-4115.629,-126.3478;Inherit;False;raodong;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;135;-5437.073,-2173.034;Inherit;True;Property;_UseDissolveMask;UseDissolveMask;46;0;Create;False;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.PannerNode;18;-4082.351,824.939;Inherit;True;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;141;-1414.066,-189.323;Inherit;False;499.0767;317.992;Comment;3;140;68;69;扰动开关;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;290;-5121.763,-1524.875;Inherit;True;Constant;_Float4;Float 4;58;0;Create;True;0;0;0;False;0;False;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;268;-1203.094,239.2005;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode;12;-1698.705,-161.038;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;106;-4534.744,-1857.75;Inherit;False;Property;_SoftaDissolve1;SoftaDissolve;41;0;Create;False;0;0;0;False;0;False;0;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;41;-4030.828,2034.274;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;264;-1526.289,489.9505;Inherit;True;0;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;7;-1187.4,342.6818;Inherit;True;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;187;-3687.331,1076.22;Inherit;False;Property;_Tex1Power;Tex1Power;16;0;Create;False;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;69;-1405.066,51.55576;Inherit;False;67;raodong;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;94;-4772.781,-1818.296;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;17;-3834.186,833.6918;Inherit;True;Property;_Tex01Mask;Tex01Mask;14;1;[Header];Create;False;1;Mask;0;0;False;0;False;-1;None;4b671ebe8c48a1443b33a99ed4f8d0b8;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;95;-4937.928,-2180.604;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;1;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;97;-4519.419,-2185.782;Inherit;True;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.PowerNode;186;-3503.104,850.5917;Inherit;True;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;93;-4271.697,-1861.202;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;269;-978.0413,267.9357;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;42;-3799.939,2005.427;Inherit;True;Property;_Tex02Mask;Tex02Mask;21;0;Create;False;1;Mask;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;265;-1185.324,558.4683;Inherit;True;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;68;-1208.221,35.01018;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SmoothstepOpNode;99;-4071.489,-2182.334;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;1,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StaticSwitch;140;-1172.761,-159.0924;Inherit;True;Property;_UseRaoDong;UseRaoDong;26;0;Create;False;0;0;0;False;1;Header(RaoDong);False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;45;-3328.048,1107.221;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;300;-788.6967,531.8846;Inherit;True;Property;_UseParticleCustom1;UseParticleCustom1XY;9;0;Create;False;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;266;-843.6505,-116.1883;Inherit;True;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;103;-3587.086,-2158.375;Inherit;True;DissolveMask;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StaticSwitch;44;-3123.086,859.0999;Inherit;False;Property;_UseMask02;UseMask02;20;0;Create;False;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;29;-2914.516,866.4049;Inherit;False;Mask01;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;107;-330,383;Inherit;True;103;DissolveMask;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;1;-623.1389,63.17684;Inherit;True;Property;_Tex01;Tex01;3;0;Create;False;0;0;0;False;0;False;-1;None;79a65c47ba3ec46469fcc011737e5d69;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;148;-319,278;Inherit;False;Constant;_Float1;Float 1;46;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;147;-91,356.9999;Inherit;False;Property;_UseDissolve;开启溶解;34;0;Create;False;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Reference;146;True;True;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;28;-182.873,162.6687;Inherit;True;29;Mask01;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;84;-559.3016,-1773.438;Inherit;False;1437.531;544.3035;;9;85;142;143;83;82;81;80;78;79;渐变贴图;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;309;135.0181,510.7217;Inherit;False;Property;_ZhouBoang;ZhouBoang;56;1;[Enum];Create;False;0;1;Make;1;0;False;0;False;1;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;48;-202.9005,51.77828;Inherit;False;Property;_UseTex01R;UseTex01R(Alpha);8;0;Create;False;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;188;-598.7321,-613.4052;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;262;139.9444,385.9771;Inherit;False;Property;_Alpha;Alpha;55;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;47;-620.9874,-323.081;Inherit;False;Property;_Tex01Color;Tex01Color;2;1;[HDR];Create;False;0;0;0;False;0;False;1,1,1,1;2.996078,2.054902,0.7686275,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;245;-6192.498,-994.3099;Inherit;False;DissolveMaskSpeed;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;43;-3249.508,2032.853;Inherit;False;Mask01;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;114;-7349.037,-1542.1;Inherit;False;Property;_DissolveRaoDong;DissolveRaoDong(OpenCanUse);45;0;Create;False;0;0;0;False;0;False;0.2;0.2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;146;574.8597,-186.0377;Inherit;True;Property;_UseDissolve;UseDissolve;34;0;Create;False;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;110;-4402.001,-232.7951;Inherit;False;DissolveRaoDong;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;79;-526.7006,-1424.395;Inherit;False;Property;_RampTexSpeedV;RampTexSpeedV;13;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;98;-4003.85,-1875.025;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;1,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StaticSwitch;142;355.9076,-1708.486;Inherit;True;Property;_UseTexRamp;UseTexRamp;10;0;Create;False;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ComponentMaskNode;193;-420.5042,-597.0328;Inherit;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;109;61.44777,-640.9056;Inherit;True;104;DissolveWidth;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;105;-4520.305,-1714.885;Inherit;False;Property;_DissolveWidth1;DissolveWidth;42;0;Create;False;0;0;0;False;0;False;0;0.1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;81;-526.9974,-1679.723;Inherit;False;0;83;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;143;66.4479,-1500.808;Inherit;False;Constant;_Float0;Float 0;44;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;102;-3510.28,-1979.24;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;113;-7262.36,-1664.158;Inherit;False;110;DissolveRaoDong;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;49;394.3552,-592.8052;Inherit;False;Property;_CullMode;CullMode;0;1;[Enum];Create;False;0;0;1;UnityEngine.Rendering.CullMode;True;0;False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;101;-3746.484,-1678.315;Inherit;False;Property;_DissolveColor1;DissolveColor;43;1;[HDR];Create;False;0;0;0;False;0;False;0,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;32;307.7437,119.862;Inherit;True;7;7;0;FLOAT;0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;96;-4128.038,-1794.793;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;87;-508.5715,-424.182;Inherit;False;85;RampTex;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;78;-529.3014,-1518.194;Inherit;False;Property;_RampTexSpeedU;RampTexSpeedU;12;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;80;-328.7203,-1477.254;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;123;-7010.046,-1618.899;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;122;-6963.673,-1750.769;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;100;-3768.91,-1980.797;Inherit;True;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;83;-8.127693,-1709.73;Inherit;True;Property;_RampTex1;RampTex1;11;0;Create;False;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;85;597.1583,-1696.11;Inherit;False;RampTex;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;51;411.4109,-429.3403;Inherit;False;Property;_dst;Add;1;1;[Enum];Create;False;0;2;AddItive;1;AlphaBlend;10;0;True;0;False;10;10;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;86;88.87781,-254.2303;Inherit;True;4;4;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;FLOAT3;0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;108;229.3631,-353.8445;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StaticSwitch;149;-7013.151,-1915.902;Inherit;False;Property;_UseRaoDong1;开启扰动效果111;26;0;Create;False;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Reference;-1;True;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;104;-3282.74,-1982.498;Inherit;True;DissolveWidth;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.PannerNode;82;-221.8286,-1680.492;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;302;1143.938,-126.4249;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ExtraPrePass;0;0;ExtraPrePass;5;False;False;False;False;False;False;False;False;True;0;False;-1;True;0;False;-1;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;False;False;False;False;False;False;True;0;False;-1;True;True;True;True;True;0;False;-1;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;0;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;306;1143.938,-126.4249;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;Meta;0;4;Meta;0;False;False;False;False;False;False;False;False;True;0;False;-1;True;0;False;-1;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;False;False;False;False;False;True;2;False;-1;False;False;False;False;False;False;False;False;True;1;LightMode=Meta;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;304;1143.938,-126.4249;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;False;False;False;False;False;True;0;False;-1;True;0;False;-1;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;False;False;False;False;True;0;False;-1;False;False;False;False;False;False;True;1;False;-1;True;3;False;-1;False;True;1;LightMode=ShadowCaster;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;303;1143.938,-126.4249;Float;False;True;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;3;VFX/UrpBest1.0;2992e84f91cbeb14eab234972e07ea9d;True;Forward;0;1;Forward;8;False;False;False;False;False;False;False;False;True;0;False;-1;True;0;True;49;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;True;2;0;True;2;5;False;-1;10;True;51;1;1;False;-1;10;False;-1;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;-1;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;2;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;LightMode=UniversalForward;False;0;Hidden/InternalErrorShader;0;0;Standard;22;Surface;1;  Blend;0;Two Sided;1;Cast Shadows;0;  Use Shadow Threshold;0;Receive Shadows;0;GPU Instancing;1;LOD CrossFade;0;Built-in Fog;0;DOTS Instancing;0;Meta Pass;0;Extra Pre Pass;0;Tessellation;0;  Phong;0;  Strength;0.5,False,-1;  Type;0;  Tess;16,False,-1;  Min;10,False,-1;  Max;25,False,-1;  Edge Length;16,False,-1;  Max Displacement;25,False,-1;Vertex Position,InvertActionOnDeselection;1;0;5;False;True;False;True;False;False;;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;305;1143.938,-126.4249;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthOnly;0;3;DepthOnly;0;False;False;False;False;False;False;False;False;True;0;False;-1;True;0;False;-1;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;False;False;False;False;0;False;-1;False;False;False;False;True;1;False;-1;False;False;True;1;LightMode=DepthOnly;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.CommentaryNode;308;904.604,480.0609;Inherit;False;100;100;firsr try;0;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;307;859.8597,304.7712;Inherit;False;525.8618;100;WeChat：15527437870_QQ:1355075263;0;MadeByZhouBoAng（If you encounter any difficulties, please contact me）;1,1,1,1;0;0
WireConnection;293;0;232;3
WireConnection;293;1;232;4
WireConnection;296;0;232;1
WireConnection;296;1;232;2
WireConnection;295;0;294;0
WireConnection;295;1;296;0
WireConnection;295;2;293;0
WireConnection;130;0;132;0
WireConnection;24;0;26;0
WireConnection;24;1;25;0
WireConnection;117;0;118;0
WireConnection;39;0;35;0
WireConnection;39;1;37;0
WireConnection;206;0;24;0
WireConnection;131;0;130;0
WireConnection;235;1;89;0
WireConnection;235;0;295;0
WireConnection;228;0;39;0
WireConnection;236;0;235;0
WireConnection;129;0;134;0
WireConnection;129;2;131;0
WireConnection;128;0;127;0
WireConnection;128;1;126;0
WireConnection;116;0;117;0
WireConnection;133;0;129;0
WireConnection;133;2;128;0
WireConnection;119;0;120;0
WireConnection;119;1;121;0
WireConnection;74;0;73;0
WireConnection;74;1;72;0
WireConnection;115;0;236;0
WireConnection;115;2;116;0
WireConnection;62;0;60;0
WireConnection;62;1;61;0
WireConnection;196;68;194;0
WireConnection;196;47;226;0
WireConnection;198;1;19;0
WireConnection;198;0;196;0
WireConnection;90;0;115;0
WireConnection;90;2;119;0
WireConnection;197;0;194;3
WireConnection;197;1;194;4
WireConnection;75;0;76;0
WireConnection;75;2;74;0
WireConnection;125;1;133;0
WireConnection;58;0;59;0
WireConnection;58;2;62;0
WireConnection;22;0;23;0
WireConnection;222;68;221;0
WireConnection;222;47;229;0
WireConnection;289;0;164;3
WireConnection;289;1;164;4
WireConnection;55;1;58;0
WireConnection;288;0;164;1
WireConnection;288;1;164;2
WireConnection;91;1;90;0
WireConnection;77;1;75;0
WireConnection;34;0;33;0
WireConnection;297;0;125;1
WireConnection;220;0;221;3
WireConnection;220;1;221;4
WireConnection;21;0;22;0
WireConnection;223;1;38;0
WireConnection;223;0;222;0
WireConnection;199;0;198;0
WireConnection;199;1;197;0
WireConnection;227;0;223;0
WireConnection;227;1;220;0
WireConnection;56;0;55;1
WireConnection;56;1;57;0
WireConnection;56;2;77;1
WireConnection;14;0;13;0
WireConnection;124;0;91;0
WireConnection;124;1;297;0
WireConnection;287;0;286;0
WireConnection;287;1;288;0
WireConnection;287;2;289;0
WireConnection;36;0;34;0
WireConnection;20;0;199;0
WireConnection;20;2;21;0
WireConnection;259;1;92;0
WireConnection;259;0;260;3
WireConnection;40;0;227;0
WireConnection;40;2;36;0
WireConnection;168;1;3;0
WireConnection;168;0;287;0
WireConnection;15;0;14;0
WireConnection;67;0;56;0
WireConnection;135;1;91;1
WireConnection;135;0;124;0
WireConnection;18;0;20;0
WireConnection;18;2;24;0
WireConnection;12;0;168;0
WireConnection;12;2;15;0
WireConnection;41;0;40;0
WireConnection;41;2;39;0
WireConnection;7;0;5;0
WireConnection;7;1;11;0
WireConnection;94;0;259;0
WireConnection;94;1;290;0
WireConnection;17;1;18;0
WireConnection;95;0;135;0
WireConnection;95;1;292;0
WireConnection;97;0;95;0
WireConnection;97;1;94;0
WireConnection;186;0;17;1
WireConnection;186;1;187;0
WireConnection;93;0;106;0
WireConnection;269;0;268;0
WireConnection;269;1;7;0
WireConnection;42;1;41;0
WireConnection;265;0;264;3
WireConnection;265;1;264;4
WireConnection;68;0;12;0
WireConnection;68;1;69;0
WireConnection;99;0;97;0
WireConnection;99;2;93;0
WireConnection;140;1;12;0
WireConnection;140;0;68;0
WireConnection;45;0;186;0
WireConnection;45;1;42;1
WireConnection;300;1;269;0
WireConnection;300;0;265;0
WireConnection;266;0;140;0
WireConnection;266;1;300;0
WireConnection;103;0;99;0
WireConnection;44;1;186;0
WireConnection;44;0;45;0
WireConnection;29;0;44;0
WireConnection;1;1;266;0
WireConnection;147;1;148;0
WireConnection;147;0;107;0
WireConnection;48;1;1;4
WireConnection;48;0;1;1
WireConnection;245;0;128;0
WireConnection;43;0;42;1
WireConnection;146;1;86;0
WireConnection;146;0;108;0
WireConnection;110;0;55;1
WireConnection;98;0;97;0
WireConnection;98;2;96;0
WireConnection;142;1;143;0
WireConnection;142;0;83;0
WireConnection;193;0;188;0
WireConnection;102;0;100;0
WireConnection;102;1;101;0
WireConnection;32;0;48;0
WireConnection;32;1;28;0
WireConnection;32;2;147;0
WireConnection;32;3;188;4
WireConnection;32;4;262;0
WireConnection;32;5;47;4
WireConnection;32;6;309;0
WireConnection;96;0;93;0
WireConnection;96;1;105;0
WireConnection;80;0;78;0
WireConnection;80;1;79;0
WireConnection;123;0;113;0
WireConnection;123;1;114;0
WireConnection;122;1;123;0
WireConnection;100;0;99;0
WireConnection;100;1;98;0
WireConnection;83;1;82;0
WireConnection;85;0;142;0
WireConnection;86;0;87;0
WireConnection;86;1;1;0
WireConnection;86;2;47;0
WireConnection;86;3;193;0
WireConnection;108;0;109;0
WireConnection;108;1;86;0
WireConnection;149;0;122;0
WireConnection;104;0;102;0
WireConnection;82;0;81;0
WireConnection;82;2;80;0
WireConnection;303;2;146;0
WireConnection;303;3;32;0
ASEEND*/
//CHKSM=A7AC51E2256A33BFDC5CADD583D56E920D0BF381