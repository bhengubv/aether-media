package aethernet.media.tv

import android.content.Context
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.cardview.widget.CardView
import androidx.leanback.widget.Presenter

/**
 * Leanback [Presenter] that renders a [ContentItem] as a focusable [CardView].
 *
 * The card shows:
 *  - Title (bold, single line)
 *  - Duration (formatted via [ContentItem.formattedDuration])
 *  - Creator tag
 */
class ContentCardPresenter : Presenter() {

    /** ViewHolder holds direct references to the card views to avoid repeated lookups. */
    class ContentViewHolder(val cardView: CardView) : ViewHolder(cardView) {
        val titleView: TextView = cardView.findViewById(android.R.id.text1)
        val subtitleView: TextView = cardView.findViewById(android.R.id.text2)
    }

    override fun onCreateViewHolder(parent: ViewGroup): ViewHolder {
        val context: Context = parent.context
        val cardView = buildCardView(context)
        cardView.isFocusable = true
        cardView.isFocusableInTouchMode = true
        return ContentViewHolder(cardView)
    }

    override fun onBindViewHolder(viewHolder: ViewHolder, item: Any) {
        val holder = viewHolder as ContentViewHolder
        val contentItem = item as ContentItem

        holder.titleView.text = contentItem.title
        holder.subtitleView.text = "${contentItem.creatorTag} · ${contentItem.formattedDuration}"
    }

    override fun onUnbindViewHolder(viewHolder: ViewHolder) {
        val holder = viewHolder as ContentViewHolder
        holder.titleView.text = null
        holder.subtitleView.text = null
    }

    /**
     * Builds the card view programmatically so no layout XML is required.
     * The card is 280×160dp and contains two stacked TextViews.
     */
    private fun buildCardView(context: Context): CardView {
        val density = context.resources.displayMetrics.density

        fun dpToPx(dp: Int): Int = (dp * density + 0.5f).toInt()

        val cardView = CardView(context).apply {
            radius = dpToPx(8).toFloat()
            cardElevation = dpToPx(4).toFloat()
            setCardBackgroundColor(0xFF2C3E50.toInt())
            layoutParams = ViewGroup.LayoutParams(dpToPx(280), dpToPx(160))
            preventCornerOverlap = true
            useCompatPadding = true
        }

        val container = android.widget.LinearLayout(context).apply {
            orientation = android.widget.LinearLayout.VERTICAL
            layoutParams = ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                ViewGroup.LayoutParams.MATCH_PARENT
            )
            val pad = dpToPx(12)
            setPadding(pad, pad, pad, pad)
            gravity = android.view.Gravity.BOTTOM
        }

        val titleView = TextView(context).apply {
            id = android.R.id.text1
            setTextColor(0xFFFFFFFF.toInt())
            textSize = 16f
            setTypeface(null, android.graphics.Typeface.BOLD)
            maxLines = 2
            ellipsize = android.text.TextUtils.TruncateAt.END
            layoutParams = android.widget.LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                ViewGroup.LayoutParams.WRAP_CONTENT
            )
        }

        val subtitleView = TextView(context).apply {
            id = android.R.id.text2
            setTextColor(0xB3FFFFFF.toInt())
            textSize = 13f
            maxLines = 1
            ellipsize = android.text.TextUtils.TruncateAt.END
            val lp = android.widget.LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                ViewGroup.LayoutParams.WRAP_CONTENT
            )
            lp.topMargin = dpToPx(4)
            layoutParams = lp
        }

        container.addView(titleView)
        container.addView(subtitleView)
        cardView.addView(container)

        return cardView
    }
}
