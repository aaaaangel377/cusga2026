#!/bin/bash

create_level() {
    local current_path="$1"
    local current_depth="$2"
    local max_depth=6
    
    if [ "$current_depth" -eq "$max_depth" ]; then
        # 最后一层，创建 Wrong.txt 文件
        touch "${current_path}/Wrong.txt"
        return
    fi
    
    # 在当前层级创建 0-9 共10个文件夹
    for i in {0..9}; do
        mkdir -p "${current_path}/${i}"
        create_level "${current_path}/${i}" $((current_depth + 1))
    done
}

BASE_DIR="Assets/Resources/computer/nested"
echo "开始创建嵌套结构到: $BASE_DIR"
create_level "$BASE_DIR" 1
echo "创建完成！"