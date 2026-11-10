# Builds the shell or one remote and serves it with Nginx.
#   docker build -f frontend/Dockerfile --build-arg APP=shell -t treasury/shell frontend
#   docker build -f frontend/Dockerfile --build-arg APP=remotes/receipts --build-arg REMOTE_BASE_URL=/remotes/receipts/ -t treasury/remote-receipts frontend
FROM node:22-alpine AS build
ARG APP=shell
ARG REMOTE_BASE_URL=/
ENV REMOTE_BASE_URL=${REMOTE_BASE_URL}
RUN corepack enable
WORKDIR /src
COPY . .
RUN pnpm install --frozen-lockfile && pnpm --filter "./${APP}" build \
 && if [ -d "${APP}/.output/public" ]; then cp -r "${APP}/.output/public" /site; else cp -r "${APP}/dist" /site; fi

FROM nginx:1.29-alpine
COPY --from=build /site /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
