# QGP Web (React/Vite) → Nginx serve tĩnh + reverse-proxy API. Build context = apps/web.
# Build (prod):
#   docker build -t qgp-web \
#     --build-arg VITE_OIDC_AUTHORITY=https://sso.example.com/realms/qgp \
#     apps/web
# syntax=docker/dockerfile:1

FROM node:20-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
# Vite bake env lúc build (chỉ biến VITE_* mới lộ ra client). Truyền qua --build-arg.
ARG VITE_API_BASE_URL=/v1
ARG VITE_USE_MOCK=0
ARG VITE_OIDC_AUTHORITY
ARG VITE_OIDC_CLIENT_ID=qgp-web
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL \
    VITE_USE_MOCK=$VITE_USE_MOCK \
    VITE_OIDC_AUTHORITY=$VITE_OIDC_AUTHORITY \
    VITE_OIDC_CLIENT_ID=$VITE_OIDC_CLIENT_ID
RUN npm run build

FROM nginx:1.27-alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80 443
