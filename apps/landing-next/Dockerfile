FROM node:20-alpine AS deps
WORKDIR /app
COPY package*.json ./
# Temporarily `npm install` instead of `npm ci` while iterating on the
# `pages` branch — the lockfile churns with @cloudflare/next-on-pages tree
# changes. ⚠ Flip back to `npm ci` before merging to master so prod catches
# drift.
RUN npm install --no-audit --no-fund

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
COPY --from=builder /app/public ./public
COPY --from=builder /app/.next/standalone ./
COPY --from=builder /app/.next/static ./.next/static
EXPOSE 3000
ENV PORT=3000
CMD ["node", "server.js"]
