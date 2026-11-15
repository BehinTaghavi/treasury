# Builds the shell or one remote and serves it with Nginx.
#   docker build -f frontend/Dockerfile --build-arg APP=shell -t treasury/shell frontend
#   docker build -f frontend/Dockerfile --build-arg APP=remotes/receipts --build-arg REMOTE_BASE_URL=/remotes/receipts/ -t treasury/remote-receipts frontend
FROM node:22-alpine AS build
WORKDIR /src

# pnpm itself, at the version pinned in package.json: its own layer, downloaded once.
COPY package.json ./
RUN corepack enable && corepack install

# Packages (~530) are downloaded into a BuildKit cache that outlives the build, so on a slow or unreliable
# connection (the owner's test PC took 21 minutes and broke off at 528 of 530) a second try only fetches what
# is missing. `sharing=locked` makes the shell and remote images, which compose builds in parallel, take turns:
# the first downloads, the others reuse. Fewer parallel downloads and longer timeouts/retries suit a slow line.
# APP is declared after this step so the install layer is identical (and cached) for every image.
COPY . .
RUN --mount=type=cache,id=treasury-pnpm-store,target=/pnpm-store,sharing=locked \
    pnpm install --frozen-lockfile --store-dir /pnpm-store \
      --network-concurrency 4 --fetch-retries 6 --fetch-retry-mintimeout 20000 \
      --fetch-retry-maxtimeout 120000 --fetch-timeout 300000

ARG APP=shell
ARG REMOTE_BASE_URL=/
ENV REMOTE_BASE_URL=${REMOTE_BASE_URL}
RUN pnpm --filter "./${APP}" build \
 && if [ -d "${APP}/.output/public" ]; then cp -r "${APP}/.output/public" /site; else cp -r "${APP}/dist" /site; fi

FROM nginx:1.29-alpine
COPY --from=build /site /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
