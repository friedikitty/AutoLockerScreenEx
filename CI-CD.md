# 构建与发布说明（CI/CD）

本文档说明本仓库的两件事：

1. `.gitignore` 如何排除构建产物，以及 git 历史里曾经混入的 exe/dll 是如何清理的；
2. GitHub Actions 自动构建与发布（Release）的流程和使用方法。

## 一、gitignore 与构建产物

`.gitignore` 内容：

```
bin/
obj/
.vs/
*.user
AutoLockerScreenEx/Assets/icon.png
```

- `bin/` 和 `obj/` 是 `dotnet build` 的全部输出目录，所有 exe/dll/pdb 都在这里面，两条规则即可排除所有构建结果，不需要单独列 `*.exe`、`*.dll`。
- 注意：**gitignore 只对"尚未被跟踪"的文件生效**。如果某个构建产物曾经被 `git add` 提交过，git 会一直跟踪它的改动，gitignore 挡不住。解决办法就是下面说的历史清理。

### 历史清理（2025 年做过一次，无需再做）

早期提交 `0052f99 "workable release"` 把 `bin/Release/` 下的 5 个构建产物提交进了仓库。清理方式：

```bash
git tag backup/pre-cleanup            # 本地备份 tag（只在本地，不推送）
git filter-branch --force --index-filter \
  "git rm -r --cached --ignore-unmatch AutoLockerScreenEx/bin" \
  --prune-empty -- main
git push --force-with-lease origin main
```

- `filter-branch` 把 `bin/` 从**每一个**历史提交中剔除，提交记录和消息原样保留（`0052f99` 因为只包含构建产物，剔除后变成空提交，被自动移除）。
- 因为历史被改写，所有提交的 hash 都变了，所以必须 `--force-with-lease` 强推覆盖远端。`--force-with-lease` 比 `--force` 安全：如果远端有你本地不知道的提交，它会拒绝覆盖。
- 万一清错了，可以用备份 tag 恢复：`git reset --hard backup/pre-cleanup`。

## 二、GitHub Actions 自动构建与发布

配置文件：`.github/workflows/build.yml`。

### 触发条件

| 操作 | 结果 |
|------|------|
| push 到 `main` 分支 | 只构建，验证代码能编译，产物存为 Artifacts（临时下载，90 天） |
| push 一个版本号 tag（`v1.0.0` 或 `1.0.0` 都行） | 构建 + **自动创建 GitHub Release**，zip 挂在 Release 页面供下载 |
| 在 GitHub 网页上点 "Draft a new release" 发布 | 同上：发布时 workflow 会自动把构建好的 zip 补充上传到这个 Release |
| 在 GitHub 网页 Actions 页手动点 "Run workflow" | 同 push 到 main |

### 流程（workflow 做了什么）

```
checkout 代码
  → 安装 .NET 10 SDK（actions/setup-dotnet）
  → dotnet publish -c Release（发布到 publish/ 目录）
  → 删除 pdb 调试文件，压缩成 AutoLockerScreenEx.zip
  → 上传 Artifacts
  → （仅版本 tag / 网页发布时）创建 Release 并附上 zip；Release 已存在则改为上传附件
```

- 运行在 GitHub 提供的 `windows-latest` 服务器上，免费（公开仓库），无需自己准备机器。
- Release 用 runner 预装的 `gh` 命令创建，`--generate-notes` 会根据上次 Release 以来的提交自动生成更新说明。
- `GITHUB_TOKEN` 是 GitHub 自动注入的密钥，不需要手动配置任何东西。

### workflow 脚本全文（下次给别的项目照这个写）

`.github/workflows/build.yml` 的完整内容：

```yaml
name: build

on:
  push:
    branches: [main]
    tags: ['v*', '[0-9]+.[0-9]+.[0-9]+']
  release:
    types: [published]
  workflow_dispatch:

permissions:
  contents: write

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Publish
        run: dotnet publish AutoLockerScreenEx/AutoLockerScreenEx.csproj -c Release -o publish

      - name: Package
        run: |
          Remove-Item publish/*.pdb
          Compress-Archive publish/* AutoLockerScreenEx.zip

      - uses: actions/upload-artifact@v4
        with:
          name: AutoLockerScreenEx
          path: AutoLockerScreenEx.zip

      - name: Release
        if: startsWith(github.ref, 'refs/tags/')
        env:
          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: gh release create ${{ github.ref_name }} AutoLockerScreenEx.zip --title ${{ github.ref_name }} --generate-notes || gh release upload ${{ github.ref_name }} AutoLockerScreenEx.zip --clobber
```

照抄时按项目改 4 处：dotnet 版本、csproj 路径、zip 文件名、`tags` 规则。几个容易踩的坑（都是这次实际踩过的）：

- **tag 规则要同时覆盖 `v*` 和纯数字 `[0-9]+.[0-9]+.[0-9]+`**：只写 `v*` 的话，打 `0.9.0` 这样的 tag 不会触发发布。
- **必须加 `release: types: [published]`**：在 GitHub 网页上点按钮创建的 tag 不产生 push 事件，只靠 `on: push: tags` 永远不会跑；加了 release 事件后网页发布也能触发。
- **发布步骤用 `create ... || upload --clobber`**：tag 推送时 Release 还不存在，走 `create`；网页发布时 Release 已存在，`create` 会失败，降级为 `upload --clobber` 把 zip 补上去。两条路共用一个步骤。
- **`permissions: contents: write` 不能少**，否则 `gh release` 没有写权限。
- **tag 触发时用的是 tag 指向的那个 commit 里的 workflow 文件**：改了 workflow 本身之后，旧 tag 需要重新指向新 commit 再推送才会用新逻辑。
- Windows runner 默认 shell 是 pwsh，`Remove-Item`、`Compress-Archive`、`||` 都直接可用；`gh` CLI 预装好了。

### 如何发布一个新版本

```bash
# 1. 正常改代码、提交、推到 main（这会先触发一次构建验证）
git add . && git commit -m "..." && git push

# 2. 确认 Actions 页构建通过后，打 tag 并推送（v1.0.0 或 1.0.0 都可以）
git tag v1.0.0
git push origin v1.0.0

# 3. 等一两分钟，GitHub 仓库的 Releases 页面就会出现 v1.0.0，附 AutoLockerScreenEx.zip
```

也可以完全在网页上操作：Releases → Draft a new release → 填一个新 tag → Publish。workflow 会在发布后被触发，自动把构建好的 zip 补充上去。

### 常见问题

- **Release 里只有 "Source code (zip/tar.gz)"？** 那是 GitHub 自动给每个 Release 附带的源码打包，不是我们的构建产物。如果缺少 `AutoLockerScreenEx.zip`，说明 workflow 没跑：原因通常是 tag 名字不符合规则（必须是 `v1.0.0` 或 `1.0.0` 这样的形式），去 Actions 页确认对应那次运行是否成功。
- **tag 打错了**：先删远端 tag（`git push origin :v1.0.0`）和 GitHub 上对应的 Release，再删本地 tag（`git tag -d v1.0.0`），改正后重新打。
- **想看某次构建的细节**：GitHub 仓库页 → Actions 标签 → 点对应的那次运行。
- **不需要 Release，只想验证能不能编译**：直接 push 到 main 即可，不要打 tag。
