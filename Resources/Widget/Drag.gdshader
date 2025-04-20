shader_type canvas_item;
uniform sampler2D gradient_texture;
uniform bool is1D;
uniform int angle; // Interpreted as angle * 90 degrees
uniform vec4 color;

void fragment() {
    // Center UVs around (0.5, 0.5) for rotation pivot
    vec2 uv = UV - vec2(0.5);
    
    // Use logic AND to wrap around (angle = 4 (0b100) would be same as angle = 0 (0b000)
    int quadrant = angle & 3;
    
    // Rotate UV coordinates based on quadrant
    vec2 rotated_uv;
    if (quadrant == 0) {
        // 0 degrees (no rotation)
        rotated_uv = uv;
    } else if (quadrant == 1) {
        // 90 degrees counter-clockwise
        rotated_uv = vec2(-uv.y, uv.x);
    } else if (quadrant == 2) {
        // 180 degrees
        rotated_uv = vec2(-uv.x, -uv.y);
    } else {
        // 270 degrees counter-clockwise (or 90 degrees clockwise)
        rotated_uv = vec2(uv.y, -uv.x);
    }
    
    // Shift back to original UV space
    rotated_uv += vec2(0.5);
    
    // If 1D gradient is in use, sample only along X axis
	if(is1D)
		rotated_uv = vec2(rotated_uv.x, 0.0);
		
    COLOR = texture(gradient_texture, rotated_uv) * color;
}