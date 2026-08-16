package com.superpos.mobile.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.runtime.Composable

private val DarkColorScheme = darkColorScheme(
    primary = FluentBlue,
    onPrimary = FluentDarkBg,
    primaryContainer = FluentBlueDark,
    secondary = FluentDarkCard,
    onSecondary = FluentWhite,
    background = FluentDarkBg,
    onBackground = FluentWhite,
    surface = FluentDarkCard,
    onSurface = FluentWhite,
    error = FluentError,
    onError = FluentDarkBg
)

@Composable
fun SuperPOSTheme(
    content: @Composable () -> Unit
) {
    MaterialTheme(
        colorScheme = DarkColorScheme,
        typography = Typography,
        content = content
    )
}
