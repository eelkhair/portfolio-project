FROM node:20-slim

WORKDIR /app
COPY package*.json ./
RUN npm ci --legacy-peer-deps


COPY . .
RUN npm run build

EXPOSE 4000
CMD ["node", "--import", "./dist/otel.js", "./dist/main.js"]
