// Aether Media TypeScript SDK — public API surface
//
// Models that share generic helper names (`toWire`, `fromWire`) are re-exported
// as namespaces so consumers can disambiguate at the call site:
//
//   import { MediaContent } from "@bhengubv/aethernet-media";
//   const wire = MediaContent.toWire(content);

export * as MediaContent  from "./models/MediaContent.js";
export * as MediaReaction from "./models/MediaReaction.js";
export * as MediaFeedItem from "./models/MediaFeedItem.js";
export * as MediaProfile  from "./models/MediaProfile.js";

// Higher-level client surfaces — flat re-export (no naming collisions).
export * from "./player/AetherNetMediaPlayer.js";
export * from "./streaming/AetherNetStreamClient.js";
export * from "./social/FeedClient.js";
export * from "./social/ReactionClient.js";
export * from "./identity/ProfileClient.js";
