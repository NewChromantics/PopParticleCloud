// Upgrade NOTE: replaced 'UNITY_INSTANCE_ID' with 'UNITY_VERTEX_INPUT_INSTANCE_ID'

Shader "Unlit/PointcloudInstanced"
{
	Properties
	{
		ParticleSize("ParticleSize",Range(0.001,1) ) = 0.1
		Radius("Radius", Range(0,1) ) = 0.5
		UseVertexColour("UseVertexColour", Range(0,1) ) = 0.0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100
		Cull off
		Blend Off

		Pass
		{
		CGPROGRAM

			//	geometry shader needs GL ES 3+
			//	https://docs.unity3d.com/Manual/SL-ShaderCompileTargets.html
			#pragma target 3.5

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing

			#include "UnityCG.cginc"

			#define lengthsq(x)	dot( (x), (x) )
			#define squared(x)	( (x)*(x) )


			struct app2vert
			{
				float4 LocalPos : POSITION;
				fixed4 Rgba : COLOR;
				fixed3 Normal : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct FragData
			{
				float4 ScreenPos : SV_POSITION;
				fixed3 LocalOffset : TEXCOORD0;
				fixed3 Colour : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			UNITY_INSTANCING_CBUFFER_START (MyProperties)
			//	UNITY_DEFINE_INSTANCED_PROP (float4, _Color)
			UNITY_INSTANCING_CBUFFER_END

			float ParticleSize;
			float Radius;
			float UseVertexColour;

			fixed3 GetParticleSize3()
			{
				return float3(ParticleSize,ParticleSize,ParticleSize);
				//	gr: thought it length of each row was scale but... doesn't seem to be. row vs col major issue?
				//float WorldScale = length(unity_ObjectToWorld[0]) + length(unity_ObjectToWorld[1]) + length(unity_ObjectToWorld[2]);
				//WorldScale /= 3.0;

				fixed3 OneTrans = mul( unity_ObjectToWorld, float4(1,0,0,0 ) );
				fixed3 ZeroTrans = mul( unity_ObjectToWorld, float4(0,0,0,0 ) );
				float WorldScale = length(OneTrans - ZeroTrans);
				fixed3 ParticleSize3 = float3( ParticleSize * WorldScale, ParticleSize * WorldScale, ParticleSize * WorldScale );
				return ParticleSize3;
			}


			FragData MakeFragData(fixed3 TriangleIndexer,float3 input_WorldPos,fixed3 input_Rgba,fixed3 ParticleSize3)
			{
				fixed isa = TriangleIndexer.x ;
				fixed isb = TriangleIndexer.y ;
				fixed isc = TriangleIndexer.z ;

				FragData x = (FragData)0;

				//	gr: use maths to work out biggest circle 
				fixed Top = -0.6;
				fixed Bottom = 0.3;
				fixed Width = 0.5;
				x.LocalOffset = isa * fixed3( 0,Top,0 );
				x.LocalOffset += isb * fixed3( -Width,Bottom,0 );
				x.LocalOffset += isc * fixed3( Width,Bottom,0 );

				float3 x_WorldPos = mul( UNITY_MATRIX_V, float4(input_WorldPos,1) ) + (x.LocalOffset * ParticleSize3);
				//float3 x_WorldPos = mul( UNITY_MATRIX_V, float3(0,0,0) ) + (x.LocalOffset * ParticleSize3);
				x.ScreenPos = mul( UNITY_MATRIX_P, float4(x_WorldPos,1) );
				x.Colour = TriangleIndexer;
				x.Colour = lerp( x.Colour, input_Rgba, UseVertexColour );
				return x;
			}

			FragData vert(app2vert v)
			{
				FragData o;
				UNITY_SETUP_INSTANCE_ID (v);
				UNITY_TRANSFER_INSTANCE_ID (v, o);

				float4 LocalPos = v.LocalPos;
				
				float3 WorldPos = mul( unity_ObjectToWorld, LocalPos );

				fixed3 ParticleSize3 = GetParticleSize3();

				o = MakeFragData( v.Normal, WorldPos, v.Rgba.xyz, ParticleSize3 );

				//o.ScreenPos = UnityObjectToClipPos( LocalPos );


				return o;
			}


			fixed4 frag(FragData i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID (i); 
				//return UNITY_ACCESS_INSTANCED_PROP (_Color);

				float DistanceFromCenterSq = lengthsq(i.LocalOffset);

				if ( DistanceFromCenterSq > squared(Radius) )
					discard;

				return fixed4( i.Colour, 1 );
			}
				ENDCG
		}
	}
}
