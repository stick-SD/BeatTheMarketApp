import csv

# Read the CSV file and generate the .designer.cs file
def generate_designer_cs(csv_file, cs_file):
    with open(csv_file, 'r') as file:
        reader = csv.DictReader(file)
        controls = list(reader)

    container_prefixes = ["tab", "group", "panel", "tabControl"]
    controls_dict = {control["Name"]: control for control in controls}

    with open(cs_file, 'w') as file:
        # Write the header
        file.write('namespace BeatTheMarketApp\n')
        file.write('{\n')
        file.write('    partial class MainForm\n')
        file.write('    {\n')
        file.write('        /// <summary>\n')
        file.write('        /// Required designer variable.\n')
        file.write('        /// </summary>\n')
        file.write('        private System.ComponentModel.IContainer components = null;\n')
        file.write('\n')
        file.write('        /// <summary>\n')
        file.write('        /// Clean up any resources being used.\n')
        file.write('        /// </summary>\n')
        file.write('        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>\n')
        file.write('        protected override void Dispose(bool disposing)\n')
        file.write('        {\n')
        file.write('            if (disposing && (components != null))\n')
        file.write('            {\n')
        file.write('                components.Dispose();\n')
        file.write('            }\n')
        file.write('            base.Dispose(disposing);\n')
        file.write('        }\n')
        file.write('\n')
        file.write('        #region Windows Form Designer generated code\n')
        file.write('\n')
        file.write('        /// <summary>\n')
        file.write('        /// Required method for Designer support - do not modify\n')
        file.write('        /// the contents of this method with the code editor.\n')
        file.write('        /// </summary>\n')
        file.write('        private void InitializeComponent()\n')
        file.write('        {\n')
        
        # Write the controls initialization
        for control in controls:
            file.write(f'            {control["Name"]} = new {control["ControlType"]}();\n')
        
        file.write('            // \n')

        # Add the SuspendLayout() calls dynamically for container controls
        for control in controls:
            if any(control["Name"].startswith(prefix) for prefix in container_prefixes):
                file.write(f'            {control["Name"]}.SuspendLayout();\n')
        file.write('            SuspendLayout();\n')
        
        # Write the properties for each control with the correct comment headers
        for control in controls:
            file.write(f'            // \n')
            file.write(f'            // {control["Name"]}\n')
            file.write(f'            // \n')

            # Add the Controls.Add() statements for container controls
            if control["ContainedControls"]:
                contained_controls = control["ContainedControls"].split(',')
                for contained_control in contained_controls:
                    file.write(f'            {control["Name"]}.Controls.Add({contained_control});\n')

            if control["Text"]:
                file.write(f'            {control["Name"]}.Text = "{control["Text"]}";\n')

            file.write(f'            {control["Name"]}.Location = new Point({control["LocationX"]}, {control["LocationY"]});\n')
            file.write(f'            {control["Name"]}.Name = "{control["Name"]}";\n')

            # Add Padding for controls starting with "tab" except "tabControl"
            if control["Name"].startswith("tab") and not control["Name"].startswith("tabControl"):
                file.write(f'            {control["Name"]}.Padding = new Padding(3);\n')

            file.write(f'            {control["Name"]}.Size = new Size({control["Width"]}, {control["Height"]});\n')
            file.write(f'            {control["Name"]}.TabIndex = 0;\n')
            file.write('            // \n')
        
        # Add the controls to the form
        file.write('            // Add controls to the form\n')
        for control in controls:
            file.write(f'            Controls.Add({control["Name"]});\n')

        file.write('\n')
        file.write('        }\n')
        file.write('\n')
        file.write('        #endregion\n')
        file.write('\n')

        # Define the controls with `private` access modifier
        for control in controls:
            file.write(f'        private {control["ControlType"]} {control["Name"]};\n')

        file.write('    }\n')
        file.write('}\n')

# Example usage
generate_designer_cs('Form_Layout_V2.csv', 'MainForm.Designer.cs')