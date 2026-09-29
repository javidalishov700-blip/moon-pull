#!/usr/bin/env bash
# Downloads the Xcode project produced by the GitHub Actions workflow "iOS Xcode project"
# and unpacks it, so a Mac runner can compile and sign it without Unity installed.
#
#   GITHUB_REPO    owner/repo, e.g. javidalishov700-blip/moon-pull
#   RELEASE_TAG    optional; defaults to ios-xcode-latest (= the repository's latest release)
#   GITHUB_TOKEN   only for a private repository: fine-grained PAT with Contents: read
set -eu

DESTINATION="${1:-ios-xcode}"
TAG="${RELEASE_TAG:-ios-xcode-latest}"
API="https://api.github.com/repos/${GITHUB_REPO}"

# github.com/<repo>/releases/latest is a plain redirect with no API rate limit.
if [ "$TAG" = "ios-xcode-latest" ]; then
  LATEST_URL="$(curl -fsSL -o /dev/null -w '%{url_effective}' "https://github.com/${GITHUB_REPO}/releases/latest" || true)"
  RESOLVED="${LATEST_URL##*/releases/tag/}"
  case "$RESOLVED" in
    ios-xcode-*) TAG="$RESOLVED" ;;
    *) echo "Could not work out the latest release from $LATEST_URL" ;;
  esac
  echo "Newest Xcode project release: $TAG"
fi

download_through_api() {
  if [ -z "${GITHUB_TOKEN:-}" ]; then
    echo "The release is not publicly downloadable and GITHUB_TOKEN is not set."
    echo "Add a fine-grained PAT (Contents: read) to the Codemagic 'unity' group."
    exit 1
  fi

  RELEASE="$(curl -fsS -H "Authorization: Bearer $GITHUB_TOKEN" \
    -H "Accept: application/vnd.github+json" "$API/releases/tags/$TAG")" || {
    echo "GitHub refused the release lookup (401 = expired or revoked GITHUB_TOKEN)."
    exit 1
  }

  ASSET_ID="$(printf '%s' "$RELEASE" | python3 -c '
import json, sys
for asset in json.load(sys.stdin).get("assets", []):
    if asset.get("name", "").endswith(".zip"):
        print(asset["id"]); break
')"

  if [ -z "$ASSET_ID" ]; then
    echo "No .zip asset on release $TAG. Run the \"iOS Xcode project\" workflow first."
    exit 1
  fi

  curl -fsSL -H "Authorization: Bearer $GITHUB_TOKEN" -H "Accept: application/octet-stream" \
    -o ios-xcode.zip "$API/releases/assets/$ASSET_ID"
}

PUBLIC_URL="https://github.com/${GITHUB_REPO}/releases/download/${TAG}/ios-xcode.zip"
echo "Downloading $PUBLIC_URL"
if ! curl -fsSL -o ios-xcode.zip "$PUBLIC_URL"; then
  echo "Direct download failed; trying the GitHub API."
  download_through_api
fi

rm -rf "$DESTINATION" .xcode-unpack
mkdir -p .xcode-unpack
unzip -q ios-xcode.zip -d .xcode-unpack

PROJECT="$(find .xcode-unpack -maxdepth 4 -name 'Unity-iPhone.xcodeproj' -print -quit)"
if [ -z "$PROJECT" ]; then
  echo "Unity-iPhone.xcodeproj not found in the archive. Top level:"
  ls -la .xcode-unpack
  exit 1
fi

mv "$(dirname "$PROJECT")" "$DESTINATION"
rm -rf .xcode-unpack

# Unity's build phases run shell scripts; make sure they stay executable.
find "$DESTINATION" -name '*.sh' -exec chmod +x {} +
echo "Xcode project ready at $DESTINATION"
