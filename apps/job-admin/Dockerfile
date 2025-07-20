# Step 1: Build Angular app
FROM node:20-alpine AS builder

WORKDIR /app

COPY package*.json ./
RUN npm install

COPY . .
RUN npm run build -- --configuration=production

# Step 2: Serve with NGINX
FROM nginx:alpine

# Copy Angular build output to NGINX's default root
COPY --from=builder /app/dist/job-admin/browser /usr/share/nginx/html

EXPOSE 60

CMD ["nginx", "-g", "daemon off;"]
