package aethermesh.media

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

private val AetherMeshBlue = Color(0xFF2196F3)
private val AetherMeshDark = Color(0xFF2C3E50)
private val AetherMeshSurface = Color(0xFF1A1A2E)

private val DarkColorScheme = darkColorScheme(
    primary = AetherMeshBlue,
    onPrimary = Color.White,
    background = AetherMeshSurface,
    surface = AetherMeshDark,
    onSurface = Color.White,
    onBackground = Color.White
)

private val LightColorScheme = lightColorScheme(
    primary = AetherMeshBlue,
    onPrimary = Color.White,
    background = Color.White,
    surface = Color(0xFFF5F5F5),
    onSurface = AetherMeshDark,
    onBackground = AetherMeshDark
)

@Composable
fun AetherMeshMediaTheme(
    darkTheme: Boolean = true,
    content: @Composable () -> Unit
) {
    val colorScheme = if (darkTheme) DarkColorScheme else LightColorScheme
    MaterialTheme(
        colorScheme = colorScheme,
        content = content
    )
}
