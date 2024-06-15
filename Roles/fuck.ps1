# 设置要遍历的目录
$dirToSearch = "D:\SharedUserFiles\Document\GitHub10\TownofHost-Enhanced\Roles"

# 检查目录是否存在
if (-Not (Test-Path $dirToSearch)) {
    Write-Host "目录不存在: $dirToSearch"
    exit 1
}

# 获取所有 .cs 文件
$files = Get-ChildItem -Path $dirToSearch -Recurse -Filter *.cs

foreach ($file in $files) {
    Write-Host "处理文件: $($file.FullName)"
    
    # 读取文件内容，使用 UTF-8 编码
    $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8
    
    # 使用正则表达式替换
    $newContent = $content -creplace "(?m)^\s*(internal\s+class\s+\S+\s*:\s*RoleBase\s*)$", "`n[Obfuscation(Exclude = true)]`n`$1"

    # 将新内容写回文件，使用 UTF-8 编码
    Set-Content -Path $file.FullName -Value $newContent -Encoding UTF8
}

Write-Host "完成处理"
