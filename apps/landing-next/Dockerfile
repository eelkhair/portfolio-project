FROM node:20-alpine AS deps
WORKDIR /app
# .npmrc sets `legacy-peer-deps=true` so npm ci doesn't try to pull the
# @cloudflare/next-on-pages peer tree (vercel CLI + 50+ @vercel/* packages).
COPY package*.json .npmrc ./
RUN npm ci

# Download the MaxMind GeoLite2 City database in an isolated stage so the
# license key never ends up in a final-image layer. The mmdb is a few tens
# of MB and gets copied into the runner stage only. If no key is provided
# at build time we create an empty sentinel and /api/geo falls back to ipapi.
FROM alpine:3.20 AS mmdb
ARG MAXMIND_LICENSE_KEY
RUN apk add --no-cache curl tar && \
    mkdir -p /mmdb && \
    if [ -z "$MAXMIND_LICENSE_KEY" ]; then \
      echo "WARN: MAXMIND_LICENSE_KEY not provided — geo will fall back to ipapi at runtime"; \
      touch /mmdb/GeoLite2-City.mmdb.missing; \
    else \
      curl -fsSL -o /tmp/geolite.tar.gz "https://download.maxmind.com/app/geoip_download?edition_id=GeoLite2-City&license_key=${MAXMIND_LICENSE_KEY}&suffix=tar.gz" && \
      tar -xzf /tmp/geolite.tar.gz -C /tmp && \
      mv /tmp/GeoLite2-City_*/GeoLite2-City.mmdb /mmdb/GeoLite2-City.mmdb && \
      rm -rf /tmp/geolite.tar.gz /tmp/GeoLite2-City_* && \
      ls -la /mmdb/GeoLite2-City.mmdb; \
    fi

FROM node:20-alpine AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
ARG NEXT_PUBLIC_TURNSTILE_SITE_KEY
ENV NEXT_PUBLIC_TURNSTILE_SITE_KEY=$NEXT_PUBLIC_TURNSTILE_SITE_KEY
ARG NEXT_PUBLIC_FARO_URL
ARG NEXT_PUBLIC_FARO_APP_NAME
ENV NEXT_PUBLIC_FARO_URL=$NEXT_PUBLIC_FARO_URL
ENV NEXT_PUBLIC_FARO_APP_NAME=$NEXT_PUBLIC_FARO_APP_NAME
RUN npm run build

FROM node:20-alpine AS runner
WORKDIR /app
ENV NODE_ENV=production
# Copy the mmdb (or the .missing sentinel) from the download stage. geo.ts
# reads from /app/geo/GeoLite2-City.mmdb at first request; missing file
# triggers a graceful fallback to ipapi.
RUN mkdir -p /app/geo
COPY --from=mmdb /mmdb/ /app/geo/
# Runtime dependencies — `maxmind` is required by geo.ts for the mmdb lookup.
# Copy just the needed modules from the builder stage.
COPY --from=builder /app/node_modules/maxmind ./node_modules/maxmind
COPY --from=builder /app/node_modules/mmdb-lib ./node_modules/mmdb-lib
COPY --from=builder /app/node_modules/tiny-lru ./node_modules/tiny-lru
COPY --from=builder /app/public ./public
COPY --from=builder /app/.next/standalone ./
COPY --from=builder /app/.next/static ./.next/static
EXPOSE 3000
ENV PORT=3000
CMD ["node", "server.js"]
