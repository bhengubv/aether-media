package aethernet.media.tv

import android.os.Bundle
import androidx.fragment.app.FragmentActivity

/**
 * Root activity for the Aether Media TV app.
 *
 * Hosts [TvMainFragment] which extends [androidx.leanback.app.BrowseSupportFragment]
 * and provides D-pad-navigable content browsing for Android TV.
 */
class MainActivity : FragmentActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        if (savedInstanceState == null) {
            supportFragmentManager
                .beginTransaction()
                .replace(android.R.id.content, TvMainFragment())
                .commit()
        }
    }
}
