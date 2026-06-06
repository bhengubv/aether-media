package aethernet.media

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Home
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material.icons.filled.Search
import androidx.compose.material3.Icon
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.navigation.NavGraph.Companion.findStartDestination
import androidx.navigation.NavHostController
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.currentBackStackEntryAsState
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import aethernet.media.ui.HomeScreen
import aethernet.media.ui.LibraryScreen
import aethernet.media.ui.NearbyScreen
import aethernet.media.ui.PlayerScreen
import aethernet.media.ui.ProfileScreen

sealed class Screen(val route: String, val label: String) {
    object Home : Screen("home", "Home")
    object Nearby : Screen("nearby", "Nearby")
    object Library : Screen("library", "Library")
    object Profile : Screen("profile", "Profile")
    object Player : Screen("player/{mediaId}", "Player") {
        fun createRoute(mediaId: String) = "player/$mediaId"
    }
}

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            AetherNetMediaTheme {
                AetherNetMediaApp()
            }
        }
    }
}

@Composable
fun AetherNetMediaApp() {
    val navController = rememberNavController()
    val bottomNavScreens = listOf(Screen.Home, Screen.Nearby, Screen.Library, Screen.Profile)

    val navBackStackEntry by navController.currentBackStackEntryAsState()
    val currentRoute = navBackStackEntry?.destination?.route

    val showBottomBar = bottomNavScreens.any { screen ->
        currentRoute == screen.route
    }

    Scaffold(
        bottomBar = {
            if (showBottomBar) {
                AetherNetBottomNav(
                    screens = bottomNavScreens,
                    currentRoute = currentRoute,
                    navController = navController
                )
            }
        }
    ) { innerPadding ->
        AetherNetNavHost(
            navController = navController,
            modifier = Modifier.padding(innerPadding)
        )
    }
}

@Composable
fun AetherNetBottomNav(
    screens: List<Screen>,
    currentRoute: String?,
    navController: NavHostController
) {
    NavigationBar {
        screens.forEach { screen ->
            val selected = currentRoute == screen.route
            NavigationBarItem(
                selected = selected,
                onClick = {
                    navController.navigate(screen.route) {
                        popUpTo(navController.graph.findStartDestination().id) {
                            saveState = true
                        }
                        launchSingleTop = true
                        restoreState = true
                    }
                },
                icon = {
                    val icon = when (screen) {
                        Screen.Home -> Icons.Filled.Home
                        Screen.Nearby -> Icons.Filled.Search
                        Screen.Library -> Icons.Filled.PlayArrow
                        Screen.Profile -> Icons.Filled.Person
                        else -> Icons.Filled.Home
                    }
                    Icon(imageVector = icon, contentDescription = screen.label)
                },
                label = { Text(screen.label) }
            )
        }
    }
}

@Composable
fun AetherNetNavHost(navController: NavHostController, modifier: Modifier = Modifier) {
    NavHost(
        navController = navController,
        startDestination = Screen.Home.route,
        modifier = modifier
    ) {
        composable(Screen.Home.route) {
            HomeScreen(
                onItemClick = { mediaId ->
                    navController.navigate(Screen.Player.createRoute(mediaId))
                }
            )
        }
        composable(Screen.Nearby.route) {
            NearbyScreen(
                onJoinStream = { mediaId ->
                    navController.navigate(Screen.Player.createRoute(mediaId))
                }
            )
        }
        composable(Screen.Library.route) {
            LibraryScreen(
                onItemClick = { mediaId ->
                    navController.navigate(Screen.Player.createRoute(mediaId))
                }
            )
        }
        composable(Screen.Profile.route) {
            ProfileScreen(
                onContentItemClick = { mediaId ->
                    navController.navigate(Screen.Player.createRoute(mediaId))
                }
            )
        }
        composable(
            route = Screen.Player.route,
            arguments = listOf(navArgument("mediaId") { type = NavType.StringType })
        ) { backStackEntry ->
            val mediaId = backStackEntry.arguments?.getString("mediaId") ?: ""
            PlayerScreen(
                mediaId = mediaId,
                onBack = { navController.popBackStack() }
            )
        }
    }
}
