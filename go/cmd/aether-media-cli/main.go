// aether-media-cli — command-line tool for scanning media directories
// and inspecting media files.
package main

import (
	"flag"
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"time"

	"github.com/bhengubv/aether-media/go/models"
	"github.com/bhengubv/aether-media/go/social"
	"github.com/bhengubv/aether-media/go/feed"
)

const version = "1.0.0"

var mediaExts = map[string]bool{
	".mp3": true, ".mp4": true, ".mkv": true, ".webm": true,
	".m4a": true, ".m4v": true, ".ogg": true, ".opus": true,
	".flac": true, ".wav": true, ".aiff": true, ".aac": true,
	".ts": true, ".mov": true, ".avi": true,
}

func main() {
	scanCmd := flag.NewFlagSet("scan", flag.ExitOnError)
	infoCmd := flag.NewFlagSet("info", flag.ExitOnError)

	if len(os.Args) < 2 {
		printUsage()
		os.Exit(1)
	}

	switch os.Args[1] {
	case "scan":
		scanCmd.Parse(os.Args[2:])
		if scanCmd.NArg() < 1 {
			fmt.Fprintln(os.Stderr, "usage: aether-media-cli scan <directory>")
			os.Exit(1)
		}
		os.Exit(cmdScan(scanCmd.Arg(0)))

	case "info":
		infoCmd.Parse(os.Args[2:])
		if infoCmd.NArg() < 1 {
			fmt.Fprintln(os.Stderr, "usage: aether-media-cli info")
			os.Exit(1)
		}
		os.Exit(cmdInfo())

	case "demo":
		// Internal demo exercising the Go packages end-to-end
		os.Exit(cmdDemo())

	case "version":
		fmt.Printf("aether-media-cli v%s\n", version)
		os.Exit(0)

	default:
		fmt.Fprintf(os.Stderr, "unknown command: %s\n", os.Args[1])
		printUsage()
		os.Exit(1)
	}
}

func printUsage() {
	fmt.Println("Aether Media CLI v" + version)
	fmt.Println()
	fmt.Println("Usage:")
	fmt.Println("  aether-media-cli scan <directory>  Walk a directory and list media files")
	fmt.Println("  aether-media-cli info               Print tool version and build info")
	fmt.Println("  aether-media-cli demo               Run an end-to-end package demo")
	fmt.Println("  aether-media-cli version            Print version")
}

// cmdScan walks the given directory and prints every media file found.
func cmdScan(dir string) int {
	info, err := os.Stat(dir)
	if err != nil || !info.IsDir() {
		fmt.Fprintf(os.Stderr, "error: '%s' is not a directory\n", dir)
		return 1
	}

	count := 0
	err = filepath.Walk(dir, func(path string, fi os.FileInfo, err error) error {
		if err != nil {
			return nil // skip unreadable entries
		}
		if fi.IsDir() {
			return nil
		}
		ext := strings.ToLower(filepath.Ext(path))
		if mediaExts[ext] {
			fmt.Printf("%s  (%d bytes)\n", path, fi.Size())
			count++
		}
		return nil
	})
	if err != nil {
		fmt.Fprintf(os.Stderr, "walk error: %v\n", err)
		return 1
	}
	if count == 0 {
		fmt.Printf("No media files found in '%s'\n", dir)
	} else {
		fmt.Printf("\n%d file(s) found.\n", count)
	}
	return 0
}

// cmdInfo prints build information.
func cmdInfo() int {
	fmt.Printf("aether-media-cli v%s\n", version)
	fmt.Printf("Module:  github.com/bhengubv/aether-media/go\n")
	fmt.Printf("Go:      see go.mod\n")
	fmt.Printf("Built:   (embedded at compile time with go build -ldflags)\n")
	return 0
}

// cmdDemo exercises the social graph, feed aggregator, and models.
func cmdDemo() int {
	fmt.Println("=== Social Graph Demo ===")
	g := social.NewSocialGraph()
	g.Follow("alice-0001")
	g.Follow("bob-0002")
	g.Follow("carol-0003")
	g.Unfollow("bob-0002")
	fmt.Printf("Following %d accounts: %v\n", g.Count(), g.Following())

	fmt.Println("\n=== MediaContent Demo ===")
	content := models.MediaContent{
		ContentHash: "e3b0c44298fc1c149afb",
		Title:       "Aether Launch Stream",
		DurationMs:  5_025_000,
		Codec:       "h264",
		ContentType: "video/mp4",
		CreatorUHID: "alice-0001",
		SizeBytes:   150_000_000,
		CreatedAtMs: time.Now().UnixMilli(),
		Tags:        []string{"aether", "launch"},
	}
	fmt.Printf("Title:    %s\n", content.Title)
	fmt.Printf("Duration: %s\n", content.FormattedDuration())
	fmt.Printf("IsVideo:  %v\n", content.IsVideo())

	fmt.Println("\n=== Feed Aggregator Demo ===")
	agg := feed.NewFeedAggregator()
	for i := 0; i < 5; i++ {
		c := content
		c.ContentHash = fmt.Sprintf("hash-%03d", i)
		c.Title = fmt.Sprintf("Video #%d", i)
		agg.AddItem(models.MediaFeedItem{
			Content:       c,
			LikeCount:     i * 10,
			PublishedAtMs: time.Now().Add(-time.Duration(i)*time.Hour).UnixMilli(),
		})
	}
	agg.MarkWatched("hash-004", 30_000)
	for _, item := range agg.GetFeed(3, 0) {
		fmt.Printf("  %s | %d likes | isNew=%v\n",
			item.Content.Title, item.LikeCount, item.IsNew())
	}

	fmt.Println("\nDemo complete.")
	return 0
}
