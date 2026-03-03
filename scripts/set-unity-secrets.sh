#!/usr/bin/env bash
set -euo pipefail

# Sets GitHub Actions secrets for Unity license activation.
# Usage: ./scripts/set-unity-secrets.sh <path-to-Unity_lic.ulf>

ULF_FILE="${1:-}"

if [ -z "$ULF_FILE" ] || [ ! -f "$ULF_FILE" ]; then
  echo "Usage: $0 <path-to-Unity_lic.ulf>"
  echo ""
  echo "Steps to get the .ulf file:"
  echo "  1. Run the 'Unity License Activation' workflow on GitHub Actions"
  echo "  2. Download the .alf artifact from the completed run"
  echo "  3. Go to https://license.unity3d.com/manual"
  echo "  4. Upload the .alf file and follow the prompts"
  echo "  5. Download the .ulf file Unity gives you"
  echo "  6. Run this script with the .ulf file path"
  exit 1
fi

echo "Setting UNITY_LICENSE secret from: $ULF_FILE"
gh secret set UNITY_LICENSE < "$ULF_FILE"

read -rp "Unity account email: " UNITY_EMAIL
gh secret set UNITY_EMAIL --body "$UNITY_EMAIL"

read -rsp "Unity account password: " UNITY_PASSWORD
echo ""
gh secret set UNITY_PASSWORD --body "$UNITY_PASSWORD"

echo ""
echo "All 3 secrets set. Re-run the 'Build Quest 3 APK' workflow:"
echo "  gh workflow run build-quest3.yml"
