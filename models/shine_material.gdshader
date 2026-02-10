shader_type spatial;

uniform sampler2D texture_albedo : source_color;
uniform float shine_intensity : hint_range(0.0, 2.0) = 0.0;
uniform vec3 shine_color : source_color = vec3(1.0, 1.0, 0.5);
uniform float shine_speed : hint_range(0.0, 10.0) = 3.0;

void fragment() {
	// Sample the base texture
	vec4 albedo_tex = texture(texture_albedo, UV);
	ALBEDO = albedo_tex.rgb;
	
	// Add pulsating shine effect when immune
	if (shine_intensity > 0.0) {
		float pulse = sin(TIME * shine_speed) * 0.5 + 0.5; // Oscillates between 0 and 1
		vec3 shine = shine_color * shine_intensity * pulse * 0.8;
		EMISSION = shine;
	} else {
		EMISSION = vec3(0.0);
	}
}
