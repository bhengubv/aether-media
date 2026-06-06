package aethermedia.tv

import android.content.Intent
import android.os.Bundle
import androidx.leanback.app.BrowseSupportFragment
import androidx.leanback.widget.ArrayObjectAdapter
import androidx.leanback.widget.HeaderItem
import androidx.leanback.widget.ListRow
import androidx.leanback.widget.ListRowPresenter
import androidx.leanback.widget.OnItemViewClickedListener

/**
 * Main browsing fragment for the Aether Media TV app.
 *
 * Displays three rows (Home, Nearby, Library) using the Leanback
 * [BrowseSupportFragment] browse layout. Each row is populated with
 * mock [ContentItem] objects and supports D-pad navigation.
 */
class TvMainFragment : BrowseSupportFragment() {

    override fun onActivityCreated(savedInstanceState: Bundle?) {
        super.onActivityCreated(savedInstanceState)

        title = "Aether Media"
        headersState = HEADERS_ENABLED
        isHeadersTransitionOnBackEnabled = true

        // Brand colour — Aether blue
        brandColor = 0xFF2196F3.toInt()

        setupRows()
        setupItemClickListener()
    }

    private fun setupRows() {
        val rowsAdapter = ArrayObjectAdapter(ListRowPresenter())

        val sections = listOf(
            "Home" to buildHomeItems(),
            "Nearby" to buildNearbyItems(),
            "Library" to buildLibraryItems()
        )

        sections.forEachIndexed { index, (label, items) ->
            val cardPresenter = ContentCardPresenter()
            val listRowAdapter = ArrayObjectAdapter(cardPresenter)
            items.forEach { listRowAdapter.add(it) }
            rowsAdapter.add(ListRow(HeaderItem(index.toLong(), label), listRowAdapter))
        }

        adapter = rowsAdapter
    }

    private fun setupItemClickListener() {
        onItemViewClickedListener = OnItemViewClickedListener { _, item, _, _ ->
            if (item is ContentItem) {
                val intent = Intent(requireContext(), TvPlayerActivity::class.java).apply {
                    putExtra(TvPlayerActivity.EXTRA_CONTENT_ID, item.id)
                    putExtra(TvPlayerActivity.EXTRA_CONTENT_TITLE, item.title)
                    putExtra(TvPlayerActivity.EXTRA_STREAM_URI, item.streamUri)
                }
                startActivity(intent)
            }
        }
    }

    // ── Mock data ─────────────────────────────────────────────────────────────

    private fun buildHomeItems(): List<ContentItem> = listOf(
        ContentItem(
            id = "tv-home-001",
            title = "African Rhythms & the Future of Sound",
            description = "A journey through the evolution of African music and how it shapes global sound.",
            durationMs = 3_660_000L,
            creatorTag = "@djkhumalo",
            streamUri = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"
        ),
        ContentItem(
            id = "tv-home-002",
            title = "Mesh Network Explained — No Internet Required",
            description = "How Aether's mesh protocol enables media sharing without a central server.",
            durationMs = 1_920_000L,
            creatorTag = "@aethertech",
            streamUri = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ElephantsDream.mp4"
        ),
        ContentItem(
            id = "tv-home-003",
            title = "Street Food Tour: Johannesburg Markets",
            description = "Exploring the vibrant street food scene across Jozi's famous markets.",
            durationMs = 2_580_000L,
            creatorTag = "@tastejozi",
            streamUri = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerBlazes.mp4"
        )
    )

    private fun buildNearbyItems(): List<ContentItem> = listOf(
        ContentItem(
            id = "tv-nearby-001",
            title = "Live: Cape Town City Session",
            description = "Live music broadcast discovered via Aether mesh — no internet required.",
            durationMs = 0L,
            creatorTag = "@sunsetgroove",
            streamUri = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"
        ),
        ContentItem(
            id = "tv-nearby-002",
            title = "Live: Underground Durban Jazz — Set 3",
            description = "Mesh-broadcast live jazz from Durban's underground scene.",
            durationMs = 0L,
            creatorTag = "@durbanflow",
            streamUri = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ElephantsDream.mp4"
        )
    )

    private fun buildLibraryItems(): List<ContentItem> = listOf(
        ContentItem(
            id = "tv-library-001",
            title = "Saved: African Rhythms",
            description = "Downloaded for offline playback via Aether mesh cache.",
            durationMs = 3_660_000L,
            creatorTag = "@djkhumalo",
            streamUri = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"
        ),
        ContentItem(
            id = "tv-library-002",
            title = "Saved: Johannesburg Markets",
            description = "Downloaded for offline playback via Aether mesh cache.",
            durationMs = 2_580_000L,
            creatorTag = "@tastejozi",
            streamUri = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ElephantsDream.mp4"
        ),
        ContentItem(
            id = "tv-library-003",
            title = "Saved: Mesh Network Explained",
            description = "Downloaded for offline playback via Aether mesh cache.",
            durationMs = 1_920_000L,
            creatorTag = "@aethertech",
            streamUri = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerBlazes.mp4"
        )
    )
}
