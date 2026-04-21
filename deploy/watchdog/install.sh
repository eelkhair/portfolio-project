#!/usr/bin/env bash
# watchdog/install.sh — bootstrap the watchdog on a fresh Debian 12 LXC.
# Idempotent: safe to re-run after config edits.
#
# Run as root on the LXC. Assumes this repo (or at least the deploy/watchdog/
# directory) is available at the path you invoke the script from.
set -euo pipefail

if [[ $EUID -ne 0 ]]; then
  echo "run as root" >&2
  exit 1
fi

SRC_DIR="$(cd "$(dirname "$0")" && pwd)"

echo "[1/6] Installing packages"
apt-get update -qq
apt-get install -qq -y curl jq openssh-client ca-certificates

echo "[2/6] Creating /opt/watchdog and /var/lib/watchdog"
install -d -m 0755 /opt/watchdog
install -d -m 0700 /opt/watchdog/.ssh
install -d -m 0755 /var/lib/watchdog

echo "[3/6] Copying check.sh"
install -m 0755 "${SRC_DIR}/check.sh" /opt/watchdog/check.sh

echo "[4/6] Seeding config.env (only if missing)"
if [[ ! -f /opt/watchdog/config.env ]]; then
  install -m 0600 "${SRC_DIR}/config.env.example" /opt/watchdog/config.env
  echo "  → /opt/watchdog/config.env created from template — EDIT IT before enabling the timer."
else
  echo "  → /opt/watchdog/config.env already exists, not overwriting."
fi

echo "[5/6] Generating SSH keypair (only if missing)"
if [[ ! -f /opt/watchdog/.ssh/id_ed25519 ]]; then
  ssh-keygen -t ed25519 -N '' -C "watchdog@$(hostname)" -f /opt/watchdog/.ssh/id_ed25519
  chmod 0600 /opt/watchdog/.ssh/id_ed25519
  chmod 0644 /opt/watchdog/.ssh/id_ed25519.pub
  echo ""
  echo "  → New public key (copy to ~eelkhair/.ssh/authorized_keys on prod + dev hosts):"
  cat /opt/watchdog/.ssh/id_ed25519.pub
  echo ""
else
  echo "  → SSH keypair already exists, not regenerating."
fi

echo "[6/6] Installing systemd units"
install -m 0644 "${SRC_DIR}/watchdog.service" /etc/systemd/system/watchdog.service
install -m 0644 "${SRC_DIR}/watchdog.timer"   /etc/systemd/system/watchdog.timer
systemctl daemon-reload

cat <<EOF

Install complete.

Before enabling the timer:
  1. Edit /opt/watchdog/config.env — fill PROD_VMID, PVE_TOKEN_SECRET, KUMA_PUSH_URL.
  2. Append /opt/watchdog/.ssh/id_ed25519.pub to ~eelkhair/.ssh/authorized_keys
     on both 192.168.1.112 (prod) and 192.168.1.200 (dev).
  3. On the dev host, grant passwordless poweroff:
       echo 'eelkhair ALL=(root) NOPASSWD: /sbin/poweroff' | sudo tee /etc/sudoers.d/watchdog
  4. Dry-run: /opt/watchdog/check.sh --dry-run
  5. Enable: systemctl enable --now watchdog.timer
  6. Watch: journalctl -u watchdog -f
EOF
