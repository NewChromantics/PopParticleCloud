// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "NewChromantics/PopParticleCloudMesh"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		ParticleSize("ParticleSize",Range(0.001,1) ) = 0.1
		Radius("Radius", Range(0,1) ) = 0.5
		UseVertexColour("UseVertexColour", Range(0,1) ) = 0.0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100
		Cull off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag

			#pragma shader_feature POINT_TOPOLOGY

			#include "UnityCG.cginc"

			struct app2vert
			{
				float4 LocalPos : POSITION;
				float2 uv : TEXCOORD0;
				float4 Rgba : COLOR;
			};

			struct vert2geo
			{
				float4 WorldPos : POSITION;
				float2 uv : TEXCOORD0;
				float4 Rgba : COLOR;
			};

			struct geo2frag
			{
				float3 LocalOffset : TEXCOORD1;
				float3 WorldPos : TEXCOORD2;
				float4 ScreenPos : SV_POSITION;
				float3 Bary : TEXCOORD3;
				float3 Colour : TEXCOORD4;
			};


			sampler2D _MainTex;
			float4 _MainTex_ST;
			float ParticleSize;
			float Radius;
			float UseVertexColour;

			vert2geo vert (app2vert v)
			{
				vert2geo o;

				float4 LocalPos = v.LocalPos;
				o.WorldPos = mul( unity_ObjectToWorld, LocalPos );

				//o.ScreenPos = mul(UNITY_MATRIX_MVP, LocalPos);

				o.uv = v.uv;
				o.Rgba = v.Rgba;

				return o;
			}

		
			#if POINT_TOPOLOGY
			[maxvertexcount(9)]
            void geom(point vert2geo _input[1], inout TriangleStream<geo2frag> OutputStream)
            {
            	int v=0;
            #else
            [maxvertexcount(9)]
            void geom(triangle vert2geo _input[3], inout TriangleStream<geo2frag> OutputStream)
            {
            	//	non-shared vertex in triangles is 2nd
            	int v=1;
            #endif

				//	get particle size in worldspace
				//	gr: thought it length of each row was scale but... doesn't seem to be. row vs col major issue?
				//float WorldScale = length(unity_ObjectToWorld[0]) + length(unity_ObjectToWorld[1]) + length(unity_ObjectToWorld[2]);
				//WorldScale /= 3.0;
				float3 OneTrans = mul( unity_ObjectToWorld, float4(1,0,0,0 ) );
				float3 ZeroTrans = mul( unity_ObjectToWorld, float4(0,0,0,0 ) );
				float WorldScale = length(OneTrans - ZeroTrans);
				float3 ParticleSize3 = float3( ParticleSize * WorldScale, ParticleSize * WorldScale, ParticleSize * WorldScale );

            	{
	            	vert2geo input = _input[v];

	                geo2frag a = (geo2frag)0;
	                geo2frag b = (geo2frag)0;
	                geo2frag c = (geo2frag)0;

	                a.Bary.xyz = float3(1,0,0);
	                b.Bary.xyz = float3(0,1,0);
	                c.Bary.xyz = float3(0,0,1);

	                //	gr: use maths to work out biggest circle 
	                float Top = -0.6;
	                float Bottom = 0.3;
	                float Width = 0.5;
	               	a.LocalOffset.xyz = float3( 0,Top,0 );
	                b.LocalOffset.xyz = float3( -Width,Bottom,0 );
	                c.LocalOffset.xyz = float3( Width,Bottom,0 );

	              
	                a.WorldPos = mul( UNITY_MATRIX_V, input.WorldPos ) + (a.LocalOffset * ParticleSize3);
	                b.WorldPos = mul( UNITY_MATRIX_V, input.WorldPos ) + (b.LocalOffset * ParticleSize3);
	                c.WorldPos = mul( UNITY_MATRIX_V, input.WorldPos ) + (c.LocalOffset * ParticleSize3);

	                a.ScreenPos = mul( UNITY_MATRIX_P, float4(a.WorldPos,1) );
	                b.ScreenPos = mul( UNITY_MATRIX_P, float4(b.WorldPos,1) );
	                c.ScreenPos = mul( UNITY_MATRIX_P, float4(c.WorldPos,1) );

	                a.Colour = a.Bary;
	                b.Colour = b.Bary;
	                c.Colour = c.Bary;

	                a.Colour = lerp( a.Colour, input.Rgba, UseVertexColour );
	                b.Colour = lerp( b.Colour, input.Rgba, UseVertexColour );
	                c.Colour = lerp( c.Colour, input.Rgba, UseVertexColour );

	              	OutputStream.Append(a);
	              	OutputStream.Append(b);
	              	OutputStream.Append(c);
	            }
            }

			
			fixed4 frag (geo2frag i) : SV_Target
			{
				//return float4( i.Bary,1  );
				float DistanceFromCenter = length(i.LocalOffset);
				//float Bary = max( i.LocalOffset.x, max( i.LocalOffset.y, i.LocalOffset.z ) );
				//float Bary = max( i.Bary.x, max( i.Bary.y, i.Bary.z ) );
				//float Bary = i.Bary.x + i.Bary.y + i.Bary.z;
				//Bary /= 3;
				//return float4( Bary, Bary, Bary, 1 );
				if ( DistanceFromCenter > Radius*ParticleSize )
				{
					//return float4(0,0,0,1);
					discard;
				}
				return float4( i.Colour, 1 );
			}
			ENDCG
		}
	}
}
