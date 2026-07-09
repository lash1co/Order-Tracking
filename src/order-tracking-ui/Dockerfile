FROM node:22-alpine AS build
WORKDIR /app
RUN corepack enable
COPY pnpm-workspace.yaml pnpm-lock.yaml ./
COPY src/order-tracking-ui/package.json src/order-tracking-ui/package.json
RUN pnpm install --filter order-tracking-ui --frozen-lockfile
COPY src/order-tracking-ui src/order-tracking-ui
RUN pnpm --filter order-tracking-ui build

FROM nginx:1.27-alpine AS runtime
COPY src/order-tracking-ui/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/src/order-tracking-ui/dist /usr/share/nginx/html
EXPOSE 8080
CMD ["nginx", "-g", "daemon off;"]
