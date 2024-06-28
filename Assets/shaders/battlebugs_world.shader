
HEADER
{
	Description = "";
}

FEATURES
{
	#include "common/features.hlsl"
}

MODES
{
	VrForward();
	Depth(); 
	ToolsVis( S_MODE_TOOLS_VIS );
	ToolsWireframe( "vr_tools_wireframe.shader" );
	ToolsShadingComplexity( "tools_shading_complexity.shader" );
}

COMMON
{
	#ifndef S_ALPHA_TEST
	#define S_ALPHA_TEST 1
	#endif
	#ifndef S_TRANSLUCENT
	#define S_TRANSLUCENT 0
	#endif
	
	#include "common/shared.hlsl"
	#include "procedural.hlsl"

	#define S_UV2 1
	#define CUSTOM_MATERIAL_INPUTS
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"
	float4 vColor : COLOR0 < Semantic( Color ); >;
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
	float3 vPositionOs : TEXCOORD14;
	float3 vNormalOs : TEXCOORD15;
	float4 vTangentUOs_flTangentVSign : TANGENT	< Semantic( TangentU_SignV ); >;
	float4 vColor : COLOR0;
	float4 vTintColor : COLOR1;
};

VS
{
	#include "common/vertex.hlsl"
	
	float g_flSway < UiGroup( ",0/,0/0" ); Default1( 0 ); Range1( 0, 1 ); >;
	float g_flSwayAmount < UiGroup( ",0/,0/0" ); Default1( 0 ); Range1( 0, 100 ); >;
	float g_flSwayMultiply < UiGroup( ",0/,0/0" ); Default1( 0 ); Range1( 0, 100 ); >;
	
	PixelInput MainVs( VertexInput v )
	{
		PixelInput i = ProcessVertex( v );
		i.vPositionOs = v.vPositionOs.xyz;
		i.vColor = v.vColor;

		ExtraShaderData_t extraShaderData = GetExtraPerInstanceShaderData( v );
		i.vTintColor = extraShaderData.vTint;

		VS_DecodeObjectSpaceNormalAndTangent( v, i.vNormalOs, i.vTangentUOs_flTangentVSign );
		
		float2 l_0 = i.vTextureCoords.xy * float2( 1, 1 );
		float l_1 = g_flSway;
		float l_2 = g_flTime * l_1;
		float l_3 = sin( l_2 );
		float l_4 = g_flSwayAmount;
		float l_5 = l_3 * l_4;
		float l_6 = 1.0f - VoronoiNoise( l_0, l_5, 3.29 );
		float l_7 = g_flSwayMultiply;
		float l_8 = l_6 * l_7;
		float l_9 = l_8.x;
		float l_10 = 0.0f;
		float4 l_11 = float4( l_9, l_10, 0, 0 );
		i.vPositionWs.xyz += l_11.xyz;
		i.vPositionPs.xyzw = Position3WsToPs( i.vPositionWs.xyz );
		
		return FinalizeVertex( i );
	}
}

PS
{
	#include "common/pixel.hlsl"
	
	SamplerState g_sSampler0 < Filter( ANISO ); AddressU( WRAP ); AddressV( WRAP ); >;
	CreateInputTexture2D( Color, Srgb, 8, "None", "_color", "Color,0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( TintMask, Srgb, 8, "None", "_mask", "Color,0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( DetailMask, Srgb, 8, "None", "_mask", "Detail,0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( TransMask, Srgb, 8, "None", "_mask", "Color,0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( Normal, Linear, 8, "None", "_normal", "PBR,0/,0/0", Default4( 0.50, 0.50, 1.00, 1.00 ) );
	CreateInputTexture2D( Rough, Srgb, 8, "None", "_color", "PBR,0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	Texture2D g_tColor < Channel( RGBA, Box( Color ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_tTintMask < Channel( RGBA, Box( TintMask ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_tDetailMask < Channel( RGBA, Box( DetailMask ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_tTransMask < Channel( RGBA, Box( TransMask ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_tNormal < Channel( RGBA, Box( Normal ), Linear ); OutputFormat( DXT5 ); SrgbRead( False ); >;
	Texture2D g_tRough < Channel( RGBA, Box( Rough ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	float g_flScale < UiGroup( "Detail,0/,0/0" ); Default1( 1 ); Range1( 0, 1000 ); >;
	float4 g_vTintColor < UiType( Color ); UiGroup( "Color,0/,0/0" ); Default4( 1.00, 1.00, 1.00, 1.00 ); >;
	float g_flDetailScale < UiGroup( "Detail,0/,0/0" ); Default1( 1 ); Range1( 0, 1000 ); >;
	float g_flBlendScale < UiGroup( "Detail,0/,0/0" ); Default1( 0.05 ); Range1( 0, 1 ); >;
	float g_flMetalness < UiGroup( "PBR,0/,0/0" ); Default1( 0 ); Range1( 0, 1 ); >;
		
	float4 TexTriplanar_Color( in Texture2D tTex, in SamplerState sSampler, float3 vPosition, float3 vNormal )
	{
		float2 uvX = vPosition.zy;
		float2 uvY = vPosition.xz;
		float2 uvZ = vPosition.xy;
	
		float3 triblend = saturate(pow(abs(vNormal), 4));
		triblend /= max(dot(triblend, half3(1,1,1)), 0.0001);
	
		half3 axisSign = vNormal < 0 ? -1 : 1;
	
		uvX.x *= axisSign.x;
		uvY.x *= axisSign.y;
		uvZ.x *= -axisSign.z;
	
		float4 colX = Tex2DS( tTex, sSampler, uvX );
		float4 colY = Tex2DS( tTex, sSampler, uvY );
		float4 colZ = Tex2DS( tTex, sSampler, uvZ );
	
		return colX * triblend.x + colY * triblend.y + colZ * triblend.z;
	}
	
	float SoftLight_blend( float a, float b )
	{
	    if ( b <= 0.5f )
	        return 2.0f * a * b + a * a * ( 1.0f * 2.0f * b );
	    else 
	        return sqrt( a ) * ( 2.0f * b - 1.0f ) + 2.0f * a * (1.0f - b);
	}
	
	float3 SoftLight_blend( float3 a, float3 b )
	{
	    return float3(
	        SoftLight_blend( a.r, b.r ),
	        SoftLight_blend( a.g, b.g ),
	        SoftLight_blend( a.b, b.b )
		);
	}
	
	float4 SoftLight_blend( float4 a, float4 b, bool blendAlpha = false )
	{
	    return float4(
	        SoftLight_blend( a.rgb, b.rgb ).rgb,
	        blendAlpha ? SoftLight_blend( a.a, b.a ) : max( a.a, b.a )
	    );
	}
	
	float3 TexTriplanar_Normal( in Texture2D tTex, in SamplerState sSampler, float3 vPosition, float3 vNormal )
	{
		float2 uvX = vPosition.zy;
		float2 uvY = vPosition.xz;
		float2 uvZ = vPosition.xy;
	
		float3 triblend = saturate( pow( abs( vNormal ), 4 ) );
		triblend /= max( dot( triblend, half3( 1, 1, 1 ) ), 0.0001 );
	
		half3 axisSign = vNormal < 0 ? -1 : 1;
	
		uvX.x *= axisSign.x;
		uvY.x *= axisSign.y;
		uvZ.x *= -axisSign.z;
	
		float3 tnormalX = DecodeNormal( Tex2DS( tTex, sSampler, uvX ).xyz );
		float3 tnormalY = DecodeNormal( Tex2DS( tTex, sSampler, uvY ).xyz );
		float3 tnormalZ = DecodeNormal( Tex2DS( tTex, sSampler, uvZ ).xyz );
	
		tnormalX.x *= axisSign.x;
		tnormalY.x *= axisSign.y;
		tnormalZ.x *= -axisSign.z;
	
		tnormalX = half3( tnormalX.xy + vNormal.zy, vNormal.x );
		tnormalY = half3( tnormalY.xy + vNormal.xz, vNormal.y );
		tnormalZ = half3( tnormalZ.xy + vNormal.xy, vNormal.z );
	
		return normalize(
			tnormalX.zyx * triblend.x +
			tnormalY.xzy * triblend.y +
			tnormalZ.xyz * triblend.z +
			vNormal
		);
	}
	
	float4 MainPs( PixelInput i ) : SV_Target0
	{
		Material m = Material::Init();
		m.Albedo = float3( 1, 1, 1 );
		m.Normal = float3( 0, 0, 1 );
		m.Roughness = 1;
		m.Metalness = 0;
		m.AmbientOcclusion = 1;
		m.TintMask = 1;
		m.Opacity = 1;
		m.Emission = float3( 0, 0, 0 );
		m.Transmission = 0;
		
		float3 l_0 = i.vPositionWithOffsetWs.xyz + g_vHighPrecisionLightingOffsetWs.xyz;
		float l_1 = g_flScale;
		float3 l_2 = l_0 / float3( l_1, l_1, l_1 );
		float4 l_3 = TexTriplanar_Color( g_tColor, g_sSampler0, l_2, i.vNormalWs );
		float4 l_4 = g_vTintColor;
		float2 l_5 = i.vTextureCoords.xy * float2( 1, 1 );
		float4 l_6 = Tex2DS( g_tTintMask, g_sSampler0, l_5 );
		float4 l_7 = saturate( lerp( l_3, l_4, l_6 ) );
		float4 l_8 = l_3 * l_7;
		float4 l_9 = i.vTintColor;
		float4 l_10 = saturate( lerp( l_7, l_9, l_6 ) );
		float4 l_11 = l_8 * l_10;
		float3 l_12 = i.vPositionWithOffsetWs.xyz + g_vHighPrecisionLightingOffsetWs.xyz;
		float l_13 = g_flDetailScale;
		float3 l_14 = l_12 / float3( l_13, l_13, l_13 );
		float4 l_15 = TexTriplanar_Color( g_tDetailMask, g_sSampler0, l_14, i.vNormalWs );
		float l_16 = g_flBlendScale;
		float4 l_17 = saturate( lerp( l_11, SoftLight_blend( l_11, l_15 ), l_16 ) );
		float2 l_18 = i.vTextureCoords.xy * float2( 1, 1 );
		float4 l_19 = Tex2DS( g_tTransMask, g_sSampler0, l_18 );
		float3 l_20 = TexTriplanar_Normal( g_tNormal, g_sSampler0, l_2, i.vNormalWs );
		float4 l_21 = TexTriplanar_Color( g_tRough, g_sSampler0, l_2, i.vNormalWs );
		float l_22 = g_flMetalness;
		
		m.Albedo = l_17.xyz;
		m.Opacity = l_19.x;
		m.Normal = l_20;
		m.Roughness = l_21.x;
		m.Metalness = l_22;
		m.AmbientOcclusion = 1;
		
		m.AmbientOcclusion = saturate( m.AmbientOcclusion );
		m.Roughness = saturate( m.Roughness );
		m.Metalness = saturate( m.Metalness );
		m.Opacity = saturate( m.Opacity );

		// Result node takes normal as tangent space, convert it to world space now
		m.Normal = TransformNormal( m.Normal, i.vNormalWs, i.vTangentUWs, i.vTangentVWs );

		// for some toolvis shit
		m.WorldTangentU = i.vTangentUWs;
		m.WorldTangentV = i.vTangentVWs;
        m.TextureCoords = i.vTextureCoords.xy;
		
		return ShadingModelStandard::Shade( i, m );
	}
}
