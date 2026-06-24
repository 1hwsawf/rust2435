# GitHub 部署说明

这个网站是纯静态页面，不需要 Node.js 构建，也不需要后端服务。把项目文件上传到 GitHub 后，可以直接用 GitHub Pages 部署。

## 文件结构

部署时保留这些文件和目录：

```text
index.html
css/
js/
assets/
plugins/
GITHUB_DEPLOY.md
```

`index.html` 是网站入口，`css/` 存放样式，`js/` 存放交互脚本，`assets/` 存放地图图片等资源，`plugins/` 存放可在线查看和复制的插件源码。

## 上传到 GitHub

在 GitHub 新建一个仓库，例如 `rust-2435-site`，然后把本文件夹里的所有网站文件上传到仓库根目录。

如果使用 Git 命令，可以在项目目录执行：

```powershell
git init
git add .
git commit -m "Deploy Rust server website"
git branch -M main
git remote add origin https://github.com/你的用户名/rust-2435-site.git
git push -u origin main
```

把命令里的 `你的用户名` 和仓库名换成你自己的 GitHub 信息。

## 开启 GitHub Pages

进入仓库页面后：

1. 打开 `Settings`
2. 进入 `Pages`
3. `Build and deployment` 选择 `Deploy from a branch`
4. `Branch` 选择 `main`
5. 目录选择 `/root`
6. 保存设置

几分钟后，GitHub 会生成一个访问地址，格式通常是：

```text
https://你的用户名.github.io/rust-2435-site/
```

## 部署后验证

打开 GitHub Pages 地址后，确认以下内容正常：

- 首页连接命令显示为 `client.connect 183.214.37.205:48930`
- 复制按钮可以复制直连命令
- 首页右侧地图卡片点击后跳转到 RustMaps 第三方地图
- 顶部 `EN / 中` 按钮可以切换中英文
- 页面会跟随系统日夜主题自动切换
- 插件卡片点击后可以打开对应源码
- 源码区域可以下载和复制 `.cs` 文件

## 注意事项

GitHub Pages 对大小写敏感。上传后不要随意修改目录名，例如 `assets`、`plugins`、`css`、`js` 必须和页面中的引用保持一致。

如果源码加载失败，通常是因为没有通过 GitHub Pages 或本地服务器访问，而是直接双击打开了 `index.html`。部署到 GitHub Pages 后，`plugins/*.cs` 文件会正常通过网页读取。
