shader_type canvas_item;

uniform vec4 outline_color : source_color = vec4(1.0);
uniform float outline_width = 1.0;
uniform bool outline_enabled = false;

void fragment() {
    vec4 vertex_color = COLOR;
    vec4 base_color = texture(TEXTURE, UV) * vertex_color;

    if (!outline_enabled) {
        COLOR = base_color;
    } else if (base_color.a > 0.05) {
        // Preserve the original sprite wherever it has visible pixels.
        COLOR = base_color;
    } else {
        // Add a border around opaque pixels. The width is expressed in
        // source-texture pixels; the C# component scales it for the sprite.
        vec2 step_uv = TEXTURE_PIXEL_SIZE * outline_width;
        float neighbor_alpha = 0.0;

        neighbor_alpha = max(neighbor_alpha, texture(TEXTURE, UV + vec2(step_uv.x, 0.0)).a);
        neighbor_alpha = max(neighbor_alpha, texture(TEXTURE, UV + vec2(-step_uv.x, 0.0)).a);
        neighbor_alpha = max(neighbor_alpha, texture(TEXTURE, UV + vec2(0.0, step_uv.y)).a);
        neighbor_alpha = max(neighbor_alpha, texture(TEXTURE, UV + vec2(0.0, -step_uv.y)).a);
        neighbor_alpha = max(neighbor_alpha, texture(TEXTURE, UV + vec2(step_uv.x, step_uv.y)).a);
        neighbor_alpha = max(neighbor_alpha, texture(TEXTURE, UV + vec2(-step_uv.x, step_uv.y)).a);
        neighbor_alpha = max(neighbor_alpha, texture(TEXTURE, UV + vec2(step_uv.x, -step_uv.y)).a);
        neighbor_alpha = max(neighbor_alpha, texture(TEXTURE, UV + vec2(-step_uv.x, -step_uv.y)).a);

        if (neighbor_alpha > 0.05) {
            COLOR = outline_color * vertex_color;
        } else {
            COLOR = vec4(0.0);
        }
    }
}
