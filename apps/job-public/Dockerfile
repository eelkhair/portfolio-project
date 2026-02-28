# Step 1: Build Angular app
FROM node:20-alpine AS builder

WORKDIR /app

COPY package*.json ./
RUN npm install

COPY . .
RUN npm run build -- --configuration=production

# Step 2: Run SSR server with Node
FROM node:20-alpine

WORKDIR /app

COPY --from=builder /app/dist/job-public .

EXPOSE 3000

ENV PORT=3000

CMD ["node", "server/server.mjs"]
