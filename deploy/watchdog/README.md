# LAN watchdog

A small LXC on the Proxmox node **opposite** the prod app host. Probes prod
every 60s and auto-recovers along a graduated escalation path:

| Fail count | Level | Action                                                     |
|------------|-------|------------------------------------------------------------|
| 3          | L1    | `ssh prod && docker compose restart`                       |
| 5          | L2    | Proxmox API `qm reboot <prod-vmid>`                        |
| 8          | L3    | `ssh dev sudo poweroff` (free cluster RAM), then retry L2  |
| ≥ 10       | —     | log + Kuma ping only; external DNS failover handles it     |

Cooldown: 5 min between escalations. Recovery resets the counter.

## One-time setup on the LXC

Start with a minimal Debian 12 (or 13) LXC. 512 MB / 1 vCPU / 4 GB disk is
plenty. Give it a static IP on the LAN.

```bash
# 1. Clone the repo (or just copy deploy/watchdog/ over)
apt-get install -y git
git clone https://github.com/Elkhair/portfolio-project /opt/portfolio-repo
cd /opt/portfolio-repo

# 2. Bootstrap — installs packages, copies scripts, generates SSH key
sudo deploy/watchdog/install.sh
```

The installer prints the watchdog's new public SSH key. Append it to
`~eelkhair/.ssh/authorized_keys` on **both** `192.168.1.112` (prod) and
`192.168.1.200` (dev).

On the **dev host only**, allow the eelkhair user to poweroff without a
password — L3 depends on this:

```bash
echo 'eelkhair ALL=(root) NOPASSWD: /sbin/poweroff' | sudo tee /etc/sudoers.d/watchdog
sudo chmod 0440 /etc/sudoers.d/watchdog
```

## Proxmox API token

Create a dedicated user + token, scoped only to the two VMs the watchdog
touches. Run on any Proxmox node as root:

```bash
# 1. Custom role with minimal privileges
pveum role add WatchdogRole -privs "VM.PowerMgmt VM.Audit Sys.Audit"

# 2. User (no shell, no password)
pveum user add watchdog@pve --comment "portfolio auto-recovery watchdog"

# 3. API token (copy the displayed secret — shown once)
pveum user token add watchdog@pve auto-recovery --privsep 1

# 4. Grant the role on the two VMs only (replace VMIDs)
pveum acl modify /vms/<PROD_VMID> --users watchdog@pve --roles WatchdogRole
pveum acl modify /vms/<DEV_VMID>  --users watchdog@pve --roles WatchdogRole
# Because --privsep 1, the token also needs the ACL:
pveum acl modify /vms/<PROD_VMID> --tokens 'watchdog@pve!auto-recovery' --roles WatchdogRole
pveum acl modify /vms/<DEV_VMID>  --tokens 'watchdog@pve!auto-recovery' --roles WatchdogRole
```

Find VMIDs with `pvesh get /cluster/resources --type vm --output-format json | jq '.[] | {vmid, name, node}'`.

Paste the VMIDs and the token secret into `/opt/watchdog/config.env`.

## Uptime Kuma

Create a **Push** monitor at `https://uptime.eelkhair.net/` — any name
(e.g. `watchdog-lan`). Kuma gives you a URL like
`https://uptime.eelkhair.net/api/push/<TOKEN>?status=up&msg=OK&ping=`.
Paste that into `KUMA_PUSH_URL` in `config.env`. The script appends
`&status=...&msg=...` per tick.

Configure Kuma's notifier (Discord/email) once; the watchdog tripping will
automatically flow through.

## Verify

```bash
# 1. Dry-run: prints what it would do, executes nothing except probes
sudo /opt/watchdog/check.sh --dry-run

# 2. Force a specific level to confirm the action path works end-to-end
sudo FORCE_LEVEL=1 /opt/watchdog/check.sh   # docker compose restart
sudo FORCE_LEVEL=2 /opt/watchdog/check.sh   # qm reboot
sudo FORCE_LEVEL=3 /opt/watchdog/check.sh   # DANGER: powers off dev

# 3. Enable the timer
sudo systemctl enable --now watchdog.timer

# 4. Watch it work
journalctl -u watchdog -f
```

State lives in `/var/lib/watchdog/state`. Delete it to reset the fail
counter after a test.

## Failure modes

- **Watchdog LXC itself dies**: Kuma's own heartbeat timeout catches it
  (missing heartbeat → Kuma marks monitor down → notifier fires). So you
  *do* get paged if the recoverer stops recovering.
- **Full `eelkhair`-node outage**: watchdog goes with it. Prod (on
  `eelkhair2`) is unaffected, and the GitHub Action DNS failover is
  independent. The watchdog is deliberately on the *opposite* node from
  prod for this reason.
- **Both Proxmox nodes down**: watchdog can't help. The GH Action will
  flip landing DNS to Cloudflare Pages within ~10 min; app services
  (`jobs`, `job-admin`, etc.) have no Pages backup and stay down.

## Rotating the watchdog out

Revoke the one SSH key on the two hosts (`~eelkhair/.ssh/authorized_keys`)
and the one Proxmox token (`pveum user token remove watchdog@pve auto-recovery`).
No other cleanup required — everything else is self-contained to the LXC.
