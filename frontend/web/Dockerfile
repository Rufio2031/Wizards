FROM node:24-alpine AS build
WORKDIR /app

# Install first, on manifests only, so dependency layers cache across source edits.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# Unprivileged variant so the final stage runs as non-root, matching the API image.
# It already defaults to USER nginx; the served port is 5173, well above 1024, so
# no privileged bind is involved.
FROM nginxinc/nginx-unprivileged:alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 5173
