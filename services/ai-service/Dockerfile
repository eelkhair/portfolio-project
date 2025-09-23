FROM node:20-slim

WORKDIR /app
COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build

EXPOSE 4000
CMD ["node", "--import", "./dist/otel.js", "./dist/index.js"]
