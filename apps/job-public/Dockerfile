# Step 1: Build Angular app
FROM node:20-alpine AS builder

ARG BUILD_CONFIG=production

WORKDIR /app

COPY package*.json ./
RUN npm install

COPY . .
RUN npm run build -- --configuration=${BUILD_CONFIG}

# Step 2: Run SSR server with Node
FROM node:20-alpine

WORKDIR /app

COPY --from=builder /app/dist/job-public .

EXPOSE 3000

ENV PORT=3000

CMD ["node", "server/server.mjs"]
