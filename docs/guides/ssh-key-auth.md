# SSH Key-Based Authentication (Passwordless Login)

Set up SSH key auth so deploy scripts can connect to remote servers without password prompts.

## 1. Check for an Existing Key

```bash
ls ~/.ssh/id_*.pub
```

If a `.pub` file exists, skip to step 3.

## 2. Generate a New Key

```bash
ssh-keygen -t ed25519
```

Press Enter for all prompts (default path, no passphrase).

This creates:
- `~/.ssh/id_ed25519` (private key -- keep this safe)
- `~/.ssh/id_ed25519.pub` (public key -- copied to servers)

## 3. Copy the Key to a Remote Server

```bash
ssh-copy-id user@<server-ip>
```

Enter the server password when prompted. This appends your public key to `~/.ssh/authorized_keys` on the server.

## 4. Verify

```bash
ssh user@<server-ip> 'echo connected'
```

Should print `connected` without asking for a password.

## Current Servers

| Environment | Host            | User     |
|-------------|-----------------|----------|
| Dev         | 192.168.1.134   | eelkhair |
| Prod        | 192.168.1.112   | eelkhair |

## Troubleshooting

If passwordless login doesn't work, check permissions on the remote server:

```bash
chmod 700 ~/.ssh
chmod 600 ~/.ssh/authorized_keys
```

Also ensure the SSH server allows key auth (should be enabled by default):

```bash
grep PubkeyAuthentication /etc/ssh/sshd_config
# Should show: PubkeyAuthentication yes
```
