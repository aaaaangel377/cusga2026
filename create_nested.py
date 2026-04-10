import os

def create_nested_structure(base_path, depth=6, current_depth=1):
    """递归创建嵌套文件夹结构"""
    if current_depth > depth:
        # 最后一层，创建 Wrong.txt 文件
        with open(os.path.join(base_path, "Wrong.txt"), "w") as f:
            f.write("")
        return
    
    # 在当前层级创建 0-9 共10个文件夹
    for i in range(10):
        folder_path = os.path.join(base_path, str(i))
        os.makedirs(folder_path, exist_ok=True)
        
        # 递归创建下一层
        create_nested_structure(folder_path, depth, current_depth + 1)

if __name__ == "__main__":
    base_dir = "Assets/Resources/computer/nested"
    print(f"开始创建嵌套结构到: {base_dir}")
    create_nested_structure(base_dir)
    print("创建完成！")