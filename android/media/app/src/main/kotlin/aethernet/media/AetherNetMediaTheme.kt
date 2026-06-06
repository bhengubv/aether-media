package aethernet.media

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

private val AetherNetBlue = Color(0xFF2196F3)
private val AetherNetDark = Color(0xFF2C3E50)
private val AetherNetSurface = Color(0xFF1A1A2E)

private val DarkColorScheme = darkColorScheme(
    primary = AetherNetBlue,
    onPrimary = Color.White,
    background = AetherNetSurface,
    surface = AetherNetDark,
    onSurface = Color.White,
    onBackground = Color.White
)

private val LightColorScheme = lightColorScheme(
    primary = AetherNetBlue,
    onPrimary = Color.White,
    background = Color.White,
    surface = Color(0xFFF5F5F5),
    onSurface = AetherNetDark,
    onBackground = AetherNetDark
)

@Composable
fun AetherNetMediaTheme(
    darkTheme: Boolean = true,
    content: @Composable () -> Unit
) {
    val colorScheme = if (darkTheme) DarkColorScheme else LightColorScheme
    MaterialTheme(
        colorScheme = colorScheme,
        content = content
    )
}
