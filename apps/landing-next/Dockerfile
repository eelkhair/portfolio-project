FROM node:20-alpine AS deps
WORKDIR /app
# .npmrc sets `legacy-peer-deps=true` so npm ci doesn't try to pull the
# @cloudflare/next-on-pages peer tree (vercel CLI + 50+ @vercel/* packages).
COPY package*.json .npmrc ./
RUN npm ci

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
